using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Customer;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("self-register")]
    [AllowAnonymous]
    public async Task<IActionResult> SelfRegister(CustomerSelfRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and Password are required.");
        }

        var exists = await _context.Users.AnyAsync(x => x.Email == request.Email);
        if (exists)
        {
            return BadRequest("Email already exists.");
        }

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

        return Ok("Customer registered successfully.");
    }

    [HttpGet("profile")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<CustomerProfileResponse>> GetProfile()
    {
        var userIdText = User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdText, out var userId))
        {
            return Unauthorized();
        }

        var customer = await _context.Users.SingleOrDefaultAsync(x => x.UserId == userId && x.Role == Roles.Customer);
        if (customer is null)
        {
            return NotFound("Customer not found.");
        }

        return Ok(new CustomerProfileResponse
        {
            UserId = customer.UserId,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Role = customer.Role,
            Address = customer.Address,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt
        });
    }

    [HttpPut("profile")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> UpdateProfile(CustomerUpdateProfileRequest request)
    {
        var userIdText = User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdText, out var userId))
        {
            return Unauthorized();
        }

        var customer = await _context.Users.SingleOrDefaultAsync(x => x.UserId == userId && x.Role == Roles.Customer);
        if (customer is null)
        {
            return NotFound("Customer not found.");
        }

        var emailInUse = await _context.Users.AnyAsync(x => x.Email == request.Email && x.UserId != userId);
        if (emailInUse)
        {
            return BadRequest("Email is already used by another user.");
        }

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        await _context.SaveChangesAsync();
        return Ok("Profile updated.");
    }
}
