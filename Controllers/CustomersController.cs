using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        var customer = await GetCurrentCustomerAsync();
        if (customer is null)
        {
            return Unauthorized();
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
        var customer = await GetCurrentCustomerAsync();
        if (customer is null)
        {
            return Unauthorized();
        }

        var emailInUse = await _context.Users.AnyAsync(x => x.Email == request.Email && x.UserId != customer.UserId);
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

    private async Task<User?> GetCurrentCustomerAsync()
    {
        var idText =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue("sub");

        if (int.TryParse(idText, out var userId))
        {
            return await _context.Users.SingleOrDefaultAsync(x => x.UserId == userId && x.Role == Roles.Customer);
        }

        var email =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Email) ??
            User.FindFirstValue("email");

        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _context.Users.SingleOrDefaultAsync(x => x.Email == email && x.Role == Roles.Customer);
        }

        return null;
    }
}
