using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Staff;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/staff/customers")]
[Authorize(Roles = Roles.Staff + "," + Roles.Admin)]
public class StaffCustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public StaffCustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<StaffCustomerSearchResult>>> SearchCustomers([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Enter a search term (name, phone, customer ID, or vehicle number).");
        }

        var term = q.Trim();
        var pattern = $"%{term}%";
        int? parsedId = int.TryParse(term, out var id) ? id : null;

        var customers = await _context.Users
            .AsNoTracking()
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Roles.Customer && u.IsActive)
            .Where(u =>
                (parsedId.HasValue && u.UserId == parsedId.Value) ||
                EF.Functions.ILike(u.Name, pattern) ||
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.Phone, pattern) ||
                u.Vehicles.Any(v => EF.Functions.ILike(v.VehicleNumber, pattern)))
            .OrderBy(u => u.Name)
            .Take(50)
            .ToListAsync();

        var results = customers.Select(u => new StaffCustomerSearchResult
        {
            UserId = u.UserId,
            Name = u.Name,
            Email = u.Email,
            Phone = u.Phone,
            Address = u.Address,
            VehicleNumbers = u.Vehicles.Select(v => v.VehicleNumber).OrderBy(x => x).ToList()
        }).ToList();

        return Ok(results);
    }

    [HttpGet("{customerId:int}")]
    public async Task<ActionResult<StaffCustomerDetailResponse>> GetCustomerDetail(int customerId)
    {
        var customer = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == customerId && x.Role == Roles.Customer);
        if (customer is null)
        {
            return NotFound("Customer not found.");
        }

        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.VehicleNumber)
            .Select(x => new StaffCustomerVehicleDetail
            {
                VehicleId = x.VehicleId,
                VehicleNumber = x.VehicleNumber,
                Brand = x.Brand,
                Model = x.Model,
                Year = x.Year
            })
            .ToListAsync();

        var purchaseHistory = await _context.SalesInvoices
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new StaffCustomerPurchaseHistoryItem
            {
                SalesInvoiceId = x.SalesInvoiceId,
                InvoiceDate = x.InvoiceDate,
                SubTotal = x.SubTotal,
                DiscountAmount = x.DiscountAmount,
                TotalAmount = x.TotalAmount,
                PaymentType = x.PaymentType,
                PaymentStatus = x.PaymentStatus,
                ItemCount = x.Items.Count
            })
            .ToListAsync();

        var serviceRows = await (
            from a in _context.Appointments.AsNoTracking()
            join v in _context.Vehicles.AsNoTracking() on a.VehicleId equals v.VehicleId
            where a.CustomerId == customerId
            orderby a.AppointmentDate descending
            select new
            {
                a.AppointmentId,
                v.Brand,
                v.Model,
                v.VehicleNumber,
                a.AppointmentDate,
                a.Status,
                a.ServiceNote
            })
            .ToListAsync();

        var serviceHistory = serviceRows.Select(x => new StaffCustomerServiceHistoryItem
        {
            AppointmentId = x.AppointmentId,
            VehicleLabel = $"{x.Brand} {x.Model} ({x.VehicleNumber})",
            AppointmentDate = x.AppointmentDate,
            Status = x.Status,
            ServiceNote = x.ServiceNote
        }).ToList();

        return Ok(new StaffCustomerDetailResponse
        {
            UserId = customer.UserId,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            CreatedAt = customer.CreatedAt,
            Vehicles = vehicles,
            PurchaseHistory = purchaseHistory,
            ServiceHistory = serviceHistory
        });
    }

    [HttpPost("register-with-vehicle")]
    public async Task<IActionResult> RegisterCustomerWithVehicle(StaffRegisterCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and Password are required.");
        }

        if (string.IsNullOrWhiteSpace(request.VehicleNumber))
        {
            return BadRequest("Vehicle number is required.");
        }

        var emailExists = await _context.Users.AnyAsync(x => x.Email == request.Email);
        if (emailExists)
        {
            return BadRequest("Customer email already exists.");
        }

        var vehicleExists = await _context.Vehicles.AnyAsync(x => x.VehicleNumber == request.VehicleNumber);
        if (vehicleExists)
        {
            return BadRequest("Vehicle number already exists.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = Roles.Customer,
                Address = request.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(customer);
            await _context.SaveChangesAsync();

            var vehicle = new Vehicle
            {
                CustomerId = customer.UserId,
                VehicleNumber = request.VehicleNumber,
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new StaffCustomerWithVehicleResponse
            {
                UserId = customer.UserId,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Role = customer.Role,
                VehicleId = vehicle.VehicleId,
                VehicleNumber = vehicle.VehicleNumber,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Failed to register customer with vehicle.");
        }
    }
}
