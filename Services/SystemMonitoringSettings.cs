namespace VehiclePartsBackend.Services;

public class SystemMonitoringSettings
{
    public int LowStockThreshold { get; set; } = 10;
    public int CreditOverdueDays { get; set; } = 30;
    public int CheckIntervalMinutes { get; set; } = 60;
}