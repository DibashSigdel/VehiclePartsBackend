using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Auth;
using VehiclePartsBackend.Models;
using VehiclePartsBackend.Services;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AppDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
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

        var token = _jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(x => x.Email == request.Email);
        if (user is null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordMatches)
        {
            return Unauthorized("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return Unauthorized("User account is inactive.");
        }

        var token = _jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        return Ok("Only admin can access this endpoint.");
    }
}
