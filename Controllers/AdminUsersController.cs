using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminUsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserResponse>>> GetUsers()
    {
        var users = await _context.Users
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminUserResponse
            {
                UserId = x.UserId,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Role = x.Role,
                Address = x.Address,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(AdminCreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and Password are required.");
        }

        if (!Roles.Allowed.Contains(request.Role))
        {
            return BadRequest("Invalid role. Use Admin, Staff, or Customer.");
        }

        var exists = await _context.Users.AnyAsync(x => x.Email == request.Email);
        if (exists)
        {
            return BadRequest("Email already exists.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            Address = request.Address,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new AdminUserResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            Address = user.Address,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{userId:int}/role")]
    public async Task<IActionResult> UpdateRole(int userId, AdminUpdateUserRoleRequest request)
    {
        if (!Roles.Allowed.Contains(request.Role))
        {
            return BadRequest("Invalid role. Use Admin, Staff, or Customer.");
        }

        var user = await _context.Users.SingleOrDefaultAsync(x => x.UserId == userId);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        user.Role = request.Role;
        await _context.SaveChangesAsync();

        return Ok("User role updated.");
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var user = await _context.Users.SingleOrDefaultAsync(x => x.UserId == userId);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var loggedInUserIdText = User.FindFirst("sub")?.Value;
        if (int.TryParse(loggedInUserIdText, out var loggedInUserId) && loggedInUserId == userId)
        {
            return BadRequest("You cannot delete your own account.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("User deleted.");
    }
}
