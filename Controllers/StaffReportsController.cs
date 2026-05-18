using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Staff;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/staff/reports")]
[Authorize(Roles = Roles.Staff + "," + Roles.Admin)]
public class StaffReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StaffReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("customers")]
    public async Task<ActionResult<StaffCustomerReportsResponse>> GetCustomerReports(
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest("Limit must be between 1 and 100.");
        }

        // SQLite cannot Sum() decimal in SQL — aggregate in memory after loading rows.
        var salesRows = await _context.SalesInvoices
            .AsNoTracking()
            .Select(x => new { x.CustomerId, x.TotalAmount, x.InvoiceDate })
            .ToListAsync();

        var customerStats = salesRows
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                InvoiceCount = g.Count(),
                TotalSpent = g.Sum(x => x.TotalAmount),
                LastPurchaseDate = g.Max(x => x.InvoiceDate)
            })
            .ToList();

        if (customerStats.Count == 0)
        {
            return Ok(new StaffCustomerReportsResponse());
        }

        var customerIds = customerStats.Select(x => x.CustomerId).ToList();
        var customers = await _context.Users
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.UserId) && x.Role == Roles.Customer)
            .Select(x => new { x.UserId, x.Name, x.Email, x.Phone })
            .ToDictionaryAsync(x => x.UserId);

        var rows = customerStats
            .Where(x => customers.ContainsKey(x.CustomerId))
            .Select(x =>
            {
                var c = customers[x.CustomerId];
                return new StaffCustomerReportRow
                {
                    CustomerId = x.CustomerId,
                    CustomerName = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    InvoiceCount = x.InvoiceCount,
                    TotalSpent = x.TotalSpent,
                    LastPurchaseDate = x.LastPurchaseDate
                };
            })
            .ToList();

        var topSpenders = rows
            .OrderByDescending(x => x.TotalSpent)
            .ThenByDescending(x => x.InvoiceCount)
            .Take(limit)
            .ToList();

        var regularCustomers = rows
            .Where(x => x.InvoiceCount >= 2)
            .OrderByDescending(x => x.InvoiceCount)
            .ThenByDescending(x => x.TotalSpent)
            .Take(limit)
            .ToList();

        var now = DateTime.UtcNow;
        var overdueRaw = await (
            from inv in _context.SalesInvoices.AsNoTracking()
            join user in _context.Users.AsNoTracking() on inv.CustomerId equals user.UserId
            where user.Role == Roles.Customer
                  && inv.PaymentStatus == "Pending"
                  && inv.CreditDueDate != null
                  && inv.CreditDueDate < now
            orderby inv.CreditDueDate
            select new
            {
                inv.SalesInvoiceId,
                inv.CustomerId,
                user.Name,
                user.Phone,
                inv.TotalAmount,
                inv.InvoiceDate,
                inv.CreditDueDate
            })
            .Take(limit)
            .ToListAsync();

        var overdueInvoices = overdueRaw
            .Select(x => new StaffOverdueCreditRow
            {
                SalesInvoiceId = x.SalesInvoiceId,
                CustomerId = x.CustomerId,
                CustomerName = x.Name,
                Phone = x.Phone,
                TotalAmount = x.TotalAmount,
                InvoiceDate = x.InvoiceDate,
                CreditDueDate = x.CreditDueDate,
                DaysOverdue = (int)(now - x.CreditDueDate!.Value).TotalDays
            })
            .ToList();

        return Ok(new StaffCustomerReportsResponse
        {
            TopSpenders = topSpenders,
            RegularCustomers = regularCustomers,
            OverdueCredits = overdueInvoices
        });
    }
}