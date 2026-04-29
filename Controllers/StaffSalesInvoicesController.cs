using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Staff;
using VehiclePartsBackend.Helpers;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/staff/sales-invoices")]
[Authorize(Roles = Roles.Staff + "," + Roles.Admin)]
public class StaffSalesInvoicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public StaffSalesInvoicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("customers")]
    public async Task<ActionResult<List<StaffCustomerOptionResponse>>> GetCustomersForSale()
    {
        var customers = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == Roles.Customer && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new StaffCustomerOptionResponse
            {
                UserId = x.UserId,
                Name = x.Name,
                Email = x.Email
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("parts")]
    public async Task<ActionResult<List<StaffPartSaleOptionResponse>>> GetPartsForSale()
    {
        var parts = await _context.Parts
            .AsNoTracking()
            .Include(x => x.Stock)
            .Where(x => x.IsActive)
            .OrderBy(x => x.PartName)
            .Select(x => new StaffPartSaleOptionResponse
            {
                PartId = x.PartId,
                PartName = x.PartName,
                SellingPrice = x.SellingPrice,
                QuantityOnHand = x.Stock != null ? x.Stock.QuantityOnHand : 0
            })
            .ToListAsync();

        return Ok(parts);
    }

    [HttpGet]
    public async Task<ActionResult<List<StaffSalesInvoiceListItemResponse>>> GetSalesInvoices()
    {
        var list = await _context.SalesInvoices
            .AsNoTracking()
            .Include(x => x.Customer)
            .OrderByDescending(x => x.InvoiceDate)
            .Take(100)
            .Select(x => new StaffSalesInvoiceListItemResponse
            {
                SalesInvoiceId = x.SalesInvoiceId,
                CustomerName = x.Customer != null ? x.Customer.Name : "",
                InvoiceDate = x.InvoiceDate,
                TotalAmount = x.TotalAmount,
                PaymentType = x.PaymentType,
                PaymentStatus = x.PaymentStatus
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<StaffSalesInvoiceResponse>> CreateSalesInvoice(StaffCreateSalesInvoiceRequest request)
    {
        var staffId = User.GetUserId();
        if (staffId is null)
        {
            return Unauthorized();
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("Add at least one line item.");
        }

        if (request.DiscountAmount < 0)
        {
            return BadRequest("Discount cannot be negative.");
        }

        var paymentType = (request.PaymentType ?? "Cash").Trim();
        var paymentStatus = (request.PaymentStatus ?? "Paid").Trim();
        if (paymentType.Length > 20 || paymentStatus.Length > 20)
        {
            return BadRequest("Payment type / status too long (max 20 characters).");
        }

        var customer = await _context.Users.SingleOrDefaultAsync(x =>
            x.UserId == request.CustomerId && x.Role == Roles.Customer && x.IsActive);
        if (customer is null)
        {
            return BadRequest("Customer not found or inactive.");
        }

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero on every line.");
            }

            if (line.UnitPrice < 0)
            {
                return BadRequest("Unit price cannot be negative.");
            }
        }

        var partIds = request.Items.Select(x => x.PartId).Distinct().ToList();
        var parts = await _context.Parts
            .Include(x => x.Stock)
            .Where(x => partIds.Contains(x.PartId))
            .ToDictionaryAsync(x => x.PartId);

        foreach (var pid in partIds)
        {
            if (!parts.TryGetValue(pid, out var part) || !part.IsActive)
            {
                return BadRequest($"Part {pid} not found or inactive.");
            }

            var qtyLine = request.Items.Where(x => x.PartId == pid).Sum(x => x.Quantity);
            var available = part.Stock?.QuantityOnHand ?? 0;
            if (available < qtyLine)
            {
                return BadRequest($"Insufficient stock for part '{part.PartName}'. Available: {available}, requested: {qtyLine}.");
            }
        }

        decimal subTotal = 0;
        foreach (var line in request.Items)
        {
            subTotal += line.UnitPrice * line.Quantity;
        }

        var totalAmount = subTotal - request.DiscountAmount;
        if (totalAmount < 0)
        {
            return BadRequest("Discount is larger than the invoice subtotal.");
        }

        var invoiceDate = request.InvoiceDate ?? DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var invoice = new SalesInvoice
            {
                CustomerId = request.CustomerId,
                CreatedByStaffId = staffId.Value,
                InvoiceDate = invoiceDate,
                PaymentType = paymentType,
                PaymentStatus = paymentStatus,
                CreditDueDate = request.CreditDueDate,
                SubTotal = subTotal,
                DiscountAmount = request.DiscountAmount,
                TotalAmount = totalAmount
            };

            foreach (var line in request.Items)
            {
                var lineTotal = line.UnitPrice * line.Quantity;
                invoice.Items.Add(new SalesInvoiceItem
                {
                    PartId = line.PartId,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    LineTotal = lineTotal
                });
            }

            _context.SalesInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var line in request.Items)
            {
                var part = parts[line.PartId];
                if (part.Stock is null)
                {
                    await tx.RollbackAsync();
                    return BadRequest($"Part '{part.PartName}' has no stock record.");
                }

                part.Stock.QuantityOnHand -= line.Quantity;
                part.Stock.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new StaffSalesInvoiceResponse
            {
                SalesInvoiceId = invoice.SalesInvoiceId,
                CustomerId = customer.UserId,
                CustomerName = customer.Name,
                InvoiceDate = invoice.InvoiceDate,
                SubTotal = invoice.SubTotal,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                PaymentType = invoice.PaymentType,
                PaymentStatus = invoice.PaymentStatus
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
