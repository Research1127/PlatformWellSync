using PlatformWellSync.Services;

namespace PlatformWellSync.Workers;

public class SyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncWorker> _logger;
    private readonly IConfiguration _config;

    public SyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SyncWorker> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var intervalMinutes = _config.GetValue<int>("Sync:IntervalMinutes", 30);

        _logger.LogInformation("SyncWorker started. Will sync every {Min} minutes.", intervalMinutes);

        while (!ct.IsCancellationRequested)
        {
            _logger.LogInformation("Sync started at {Time}", DateTimeOffset.Now);

            try
            {
                // Create a fresh scope for each sync run
                using var scope = _scopeFactory.CreateScope();

                var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

                await syncService.RunAsync();
            }
            catch (Exception ex)
            {
                // Log the error but DO NOT crash
                // Worker keeps running and will retry next interval
                _logger.LogError(ex, "Sync failed at {Time}. Will retry in {Min} minutes.",
                    DateTimeOffset.Now, intervalMinutes);
            }

            _logger.LogInformation("Next sync in {Min} minutes.", intervalMinutes);

            // Wait for next interval — respects cancellation (Ctrl+C)
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), ct);
        }

        _logger.LogInformation("SyncWorker stopped.");
    }
}