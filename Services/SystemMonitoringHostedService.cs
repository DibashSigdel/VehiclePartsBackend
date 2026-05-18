using Microsoft.Extensions.Options;

namespace VehiclePartsBackend.Services;

public class SystemMonitoringHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SystemMonitoringSettings _settings;
    private readonly ILogger<SystemMonitoringHostedService> _logger;

    public SystemMonitoringHostedService(
        IServiceProvider serviceProvider,
        IOptions<SystemMonitoringSettings> settings,
        ILogger<SystemMonitoringHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.CheckIntervalMinutes));
        _logger.LogInformation("System monitoring started. Interval: {Minutes} minute(s).", interval.TotalMinutes);

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var monitoring = scope.ServiceProvider.GetRequiredService<SystemMonitoringService>();
                var result = await monitoring.RunChecksAsync(stoppingToken);
                _logger.LogInformation(
                    "Automatic system check: {Message}",
                    result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic system monitoring failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}