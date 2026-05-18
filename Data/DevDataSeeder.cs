using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Data;

public static class DevDataSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Seeding development users (empty database)...");

        var users = new[]
        {
            new User
            {
                Name = "System Admin",
                Email = "admin@vehicleparts.com",
                Phone = "9800000001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = Roles.Admin,
                Address = "Kathmandu",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Name = "Staff User",
                Email = "staff@vehicleparts.com",
                Phone = "9800000002",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff123"),
                Role = Roles.Staff,
                Address = "Kathmandu",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Name = "Test Customer",
                Email = "customer@vehicleparts.com",
                Phone = "9800000003",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("customer123"),
                Role = Roles.Customer,
                Address = "Kathmandu",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var customer = await context.Users.SingleAsync(u => u.Email == "customer@vehicleparts.com");
        context.Vehicles.Add(new Vehicle
        {
            CustomerId = customer.UserId,
            VehicleNumber = "BA-1-PA-1234",
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Development seed complete. Login: admin@vehicleparts.com / admin123");
    }
}
