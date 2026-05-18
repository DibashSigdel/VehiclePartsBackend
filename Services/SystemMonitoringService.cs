using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VehiclePartsBackend.Constants;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Dtos.Admin;
using VehiclePartsBackend.Models;

namespace VehiclePartsBackend.Services;

public class SystemMonitoringService
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly SystemMonitoringSettings _settings;
    private readonly ILogger<SystemMonitoringService> _logger;

    public SystemMonitoringService(
        AppDbContext context,
        IEmailSender emailSender,
        IOptions<SystemMonitoringSettings> settings,
        ILogger<SystemMonitoringService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SystemMonitoringRunResponse> RunChecksAsync(CancellationToken cancellationToken = default)
    {
        var lowStockCreated = await ProcessLowStockAlertsAsync(cancellationToken);
        var creditEmailsSent = await ProcessOverdueCreditRemindersAsync(cancellationToken);
        var lowStockParts = await GetLowStockPartsAsync(cancellationToken);

        return new SystemMonitoringRunResponse
        {
            LowStockNotificationsCreated = lowStockCreated,
            CreditReminderEmailsSent = creditEmailsSent,
            LowStockParts = lowStockParts,
            Message = $"Checks complete. {lowStockCreated} admin notification(s), {creditEmailsSent} credit reminder email(s)."
        };
    }

    public async Task<List<LowStockAlertResponse>> GetLowStockPartsAsync(CancellationToken cancellationToken = default)
    {
        var threshold = _settings.LowStockThreshold;
        return await (
            from stock in _context.Stocks.AsNoTracking()
            join part in _context.Parts.AsNoTracking() on stock.PartId equals part.PartId
            where part.IsActive && stock.QuantityOnHand < threshold
            orderby stock.QuantityOnHand, part.PartName
            select new LowStockAlertResponse
            {
                PartId = part.PartId,
                PartName = part.PartName,
                QuantityOnHand = stock.QuantityOnHand
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<int> ProcessLowStockAlertsAsync(CancellationToken cancellationToken)
    {
        var lowStockParts = await GetLowStockPartsAsync(cancellationToken);
        if (lowStockParts.Count == 0)
        {
            return 0;
        }

        var adminIds = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == Roles.Admin && x.IsActive)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        if (adminIds.Count == 0)
        {
            _logger.LogWarning("No active admin users to receive low-stock notifications.");
            return 0;
        }

        var since = DateTime.UtcNow.AddHours(-24);
        var created = 0;

        foreach (var part in lowStockParts)
        {
            var message =
                $"Low stock: '{part.PartName}' (part #{part.PartId}) has {part.QuantityOnHand} unit(s) left (threshold: {_settings.LowStockThreshold}).";

            foreach (var adminId in adminIds)
            {
                var alreadyNotified = await _context.Notifications.AnyAsync(
                    x => x.UserId == adminId
                         && x.NotificationType == "LowStock"
                         && x.Message == message
                         && x.SentAt >= since,
                    cancellationToken);

                if (alreadyNotified)
                {
                    continue;
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    NotificationType = "LowStock",
                    Message = message,
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                });
                created++;
            }
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private async Task<int> ProcessOverdueCreditRemindersAsync(CancellationToken cancellationToken)
    {
        var overdueCutoff = DateTime.UtcNow.AddDays(-_settings.CreditOverdueDays);
        var reminderCooldown = DateTime.UtcNow.AddDays(-_settings.CreditOverdueDays);

        var overdueInvoices = await (
            from inv in _context.SalesInvoices.AsNoTracking()
            join customer in _context.Users.AsNoTracking() on inv.CustomerId equals customer.UserId
            where inv.PaymentStatus == "Pending"
                  && inv.CreditDueDate != null
                  && inv.CreditDueDate < overdueCutoff
                  && !string.IsNullOrEmpty(customer.Email)
            select new
            {
                inv.SalesInvoiceId,
                inv.CustomerId,
                inv.TotalAmount,
                inv.CreditDueDate,
                customer.Name,
                customer.Email
            })
            .ToListAsync(cancellationToken);

        var emailsSent = 0;

        foreach (var inv in overdueInvoices)
        {
            var recentlyReminded = await _context.CreditReminders.AnyAsync(
                x => x.SalesInvoiceId == inv.SalesInvoiceId && x.ReminderSentAt >= reminderCooldown,
                cancellationToken);

            if (recentlyReminded)
            {
                continue;
            }

            var dueDate = inv.CreditDueDate!.Value;
            var subject = $"Payment reminder — invoice #{inv.SalesInvoiceId}";
            var html = $"""
                <html><body style="font-family:Segoe UI,Arial,sans-serif">
                <h2>Payment reminder</h2>
                <p>Dear {System.Net.WebUtility.HtmlEncode(inv.Name)},</p>
                <p>Your credit payment for invoice <strong>#{inv.SalesInvoiceId}</strong> is overdue.</p>
                <p>Amount due: <strong>{inv.TotalAmount:N2}</strong><br/>
                Original due date: <strong>{dueDate:yyyy-MM-dd}</strong></p>
                <p>Please contact the Vehicle Service Center to arrange payment.</p>
                <p>Regards,<br/>Vehicle Parts System</p>
                </body></html>
                """;

            await _emailSender.SendAsync(inv.Email, subject, html, cancellationToken);

            _context.CreditReminders.Add(new CreditReminder
            {
                SalesInvoiceId = inv.SalesInvoiceId,
                CustomerId = inv.CustomerId,
                AmountDue = inv.TotalAmount,
                ReminderSentAt = DateTime.UtcNow
            });

            emailsSent++;
        }

        if (emailsSent > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return emailsSent;
    }
}