using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;
using VehiclePartsBackend.Helpers;
using VehiclePartsBackend.Services;

namespace VehiclePartsBackend.Controllers;

[ApiController]
[Route("api/admin/system")]
[Authorize(Roles = Roles.Admin)]
public class AdminSystemAlertsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SystemMonitoringService _monitoringService;

    public AdminSystemAlertsController(AppDbContext context, SystemMonitoringService monitoringService)
    {
        _context = context;
        _monitoringService = monitoringService;
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockAlertResponse>>> GetLowStock()
    {
        return Ok(await _monitoringService.GetLowStockPartsAsync());
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<List<AdminNotificationResponse>>> GetMyNotifications()
    {
        var adminId = User.GetUserId();
        if (adminId is null)
        {
            return Unauthorized();
        }

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == adminId.Value)
            .OrderByDescending(x => x.SentAt)
            .Take(100)
            .Select(x => new AdminNotificationResponse
            {
                NotificationId = x.NotificationId,
                NotificationType = x.NotificationType,
                Message = x.Message,
                IsRead = x.IsRead,
                SentAt = x.SentAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPost("notifications/{notificationId:int}/read")]
    public async Task<IActionResult> MarkNotificationRead(int notificationId)
    {
        var adminId = User.GetUserId();
        if (adminId is null)
        {
            return Unauthorized();
        }

        var notification = await _context.Notifications
            .SingleOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == adminId.Value);

        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok("Notification marked as read.");
    }

    [HttpPost("run-checks")]
    public async Task<ActionResult<SystemMonitoringRunResponse>> RunChecks()
    {
        var result = await _monitoringService.RunChecksAsync();
        return Ok(result);
    }
}