namespace VehiclePartsBackend.Dtos.Admin;

public class AdminNotificationResponse
{
    public int NotificationId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}

public class LowStockAlertResponse
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
}

public class SystemMonitoringRunResponse
{
    public int LowStockNotificationsCreated { get; set; }
    public int CreditReminderEmailsSent { get; set; }
    public List<LowStockAlertResponse> LowStockParts { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}