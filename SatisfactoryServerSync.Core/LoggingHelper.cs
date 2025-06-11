using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.TraceSource;

namespace SatisfactoryServerSync.Core;

/// <summary>
/// Helper for setting up trace listeners for the application
/// </summary>
public static class LoggingHelper
{
    /// <summary>
    /// Sets up trace listeners based on configuration
    /// </summary>
    /// <param name="config">Application configuration</param>
    /// <param name="includeConsole">Whether to include console trace listener</param>
    public static void SetupTraceListeners(SyncConfiguration config, bool includeConsole = false)
    {
        // Clear existing listeners
        Trace.Listeners.Clear();

        // Add console listener if requested (for console app)
        if (includeConsole)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        // Add file listener
        if (!string.IsNullOrWhiteSpace(config.Logging.LogFilePath))
        {
            var expandedLogPath = Environment.ExpandEnvironmentVariables(config.Logging.LogFilePath);
            var fileListener = new FileTraceListener(expandedLogPath);
            Trace.Listeners.Add(fileListener);
        }

        // Set trace level based on configuration
        if (Enum.TryParse<SourceLevels>(config.Logging.LogLevel, true, out var level))
        {
            Trace.AutoFlush = true;
        }
    }    /// <summary>
    /// Creates a logger factory with console and file logging
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory(SyncConfiguration config)
    {
        return LoggerFactory.Create(builder =>
        {
            // Add console logging
            builder.AddConsole();
            
            // Set minimum log level
            if (Enum.TryParse<LogLevel>(config.Logging.LogLevel, true, out var logLevel))
            {
                builder.SetMinimumLevel(logLevel);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Information);
            }
        });
    }    /// <summary>
    /// Logs application startup information
    /// </summary>
    public static void LogStartup(ILogger logger, string applicationName, SyncConfiguration config)
    {
        logger.LogInformation("=== {ApplicationName} Started ===", applicationName);
        logger.LogInformation("Version: {Version}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
        logger.LogInformation("Start Time: {StartTime}", DateTime.Now);
        logger.LogInformation("Configuration:");
        logger.LogInformation("  Container: {Container}", config.AzureStorage.ContainerName);
        logger.LogInformation("  Process Name: {ProcessName}", config.SatisfactoryGame.ProcessName);
        logger.LogInformation("  Save File: {SaveFile}", $"Directory: {config.SatisfactoryGame.SaveFileDirectory}, Prefix: {config.SatisfactoryGame.SaveFilePrefix}");
        logger.LogInformation("  Check Interval: {Interval} minutes", config.Synchronization.CheckIntervalMinutes);
        logger.LogInformation("  Log Level: {LogLevel}", config.Logging.LogLevel);
        logger.LogInformation("====================================");
    }

    /// <summary>
    /// Logs application shutdown information
    /// </summary>
    public static void LogShutdown(ILogger logger, string applicationName)
    {
        logger.LogInformation("=== {ApplicationName} Shutting Down ===", applicationName);
        logger.LogInformation("End Time: {EndTime}", DateTime.Now);
        logger.LogInformation("=========================================");
    }
}
