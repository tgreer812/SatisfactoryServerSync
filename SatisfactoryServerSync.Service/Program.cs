using SatisfactoryServerSync.Core;
using SatisfactoryServerSync.Service;

var builder = Host.CreateApplicationBuilder(args);

try
{
    // Load configuration
    var config = ConfigurationHelper.LoadConfiguration();
    
    // Register configuration as singleton
    builder.Services.AddSingleton(config);
    
    // Register the sync service
    builder.Services.AddSingleton<SatisfactorySyncService>();
    
    // Register the worker service
    builder.Services.AddHostedService<Worker>();
    
    // Configure as Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "SatisfactoryServerSync";
    });

    // Setup logging
    builder.Services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        
        // Add file logging
        var expandedLogPath = Environment.ExpandEnvironmentVariables(config.Logging.LogFilePath);
        var fileListener = new FileTraceListener(expandedLogPath);
        
        logging.AddTraceSource(new System.Diagnostics.SourceSwitch("SatisfactorySync", config.Logging.LogLevel), fileListener);
        
        // Log the raw log level value for debugging
        Console.WriteLine($"[DEBUG] config.Logging.LogLevel raw value: '{config.Logging.LogLevel}'");
        
        // Trim whitespace before parsing log level
        var trimmedLogLevel = config.Logging.LogLevel?.Trim();
        if (Enum.TryParse<LogLevel>(trimmedLogLevel, true, out var logLevel))
        {
            logging.SetMinimumLevel(logLevel);
        }
        else
        {
            Console.WriteLine($"[WARN] Invalid log level in config: '{config.Logging.LogLevel}', defaulting to Information.");
            logging.SetMinimumLevel(LogLevel.Information);
        }
    });

    var host = builder.Build();
    
    // Setup trace listeners for the service
    LoggingHelper.SetupTraceListeners(config, includeConsole: false);
    
    await host.RunAsync();
}
catch (Exception ex)
{
    // Log to Windows Event Log if our logging isn't set up yet (Windows only)
    if (OperatingSystem.IsWindows())
    {
        try
        {
            using var eventLog = new System.Diagnostics.EventLog("Application");
            eventLog.Source = "SatisfactoryServerSync";
            eventLog.WriteEntry($"Fatal error starting SatisfactoryServerSync service: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                System.Diagnostics.EventLogEntryType.Error);
        }
        catch
        {
            // If we can't even write to event log, there's nothing more we can do
        }
    }
    
    // Also write to console as fallback
    Console.WriteLine($"Fatal error starting SatisfactoryServerSync service: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    
    throw;
}
