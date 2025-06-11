using SatisfactoryServerSync.Core;

namespace SatisfactoryServerSync.Service;

/// <summary>
/// Background service that continuously synchronizes Satisfactory save files
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly SatisfactorySyncService _syncService;
    private readonly SyncConfiguration _config;

    public Worker(ILogger<Worker> logger, SatisfactorySyncService syncService, SyncConfiguration config)
    {
        _logger = logger;
        _syncService = syncService;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SatisfactoryServerSync service starting up");
        LoggingHelper.LogStartup(_logger, "SatisfactoryServerSync Service", _config);

        var interval = TimeSpan.FromMinutes(_config.Synchronization.CheckIntervalMinutes);
        _logger.LogInformation("Sync interval set to {Interval} minutes", _config.Synchronization.CheckIntervalMinutes);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _syncService.SynchronizeAsync(stoppingToken);
                    
                    // Only log if something interesting happened
                    if (result.Action != SyncAction.None && result.Action != SyncAction.Skipped)
                    {
                        _logger.LogInformation("Sync completed: {Action} - {Message}", result.Action, result.Message);
                    }
                    else
                    {
                        _logger.LogDebug("Sync check: {Action} - {Message}", result.Action, result.Message);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during synchronization cycle");
                    // Continue running despite errors - they should be transient
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation("SatisfactoryServerSync service stopping");
            LoggingHelper.LogShutdown(_logger, "SatisfactoryServerSync Service");
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SatisfactoryServerSync service start requested");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SatisfactoryServerSync service stop requested");
        await base.StopAsync(cancellationToken);
    }
}
