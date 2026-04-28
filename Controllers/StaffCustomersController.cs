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
