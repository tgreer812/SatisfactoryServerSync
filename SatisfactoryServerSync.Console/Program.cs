using Microsoft.Extensions.Logging;
using SatisfactoryServerSync.Core;
using System.Diagnostics;

namespace SatisfactoryServerSync.Console;

class Program
{
    private static ILogger<Program>? _logger;

    static async Task<int> Main(string[] args)
    {
        System.Console.WriteLine("=== SatisfactoryServerSync Console ===");
        System.Console.WriteLine("Debug/testing console for Satisfactory save file synchronization");
        System.Console.WriteLine();

        try
        {
            // Load configuration
            var config = LoadConfiguration(args);
            
            // Setup logging with console output
            LoggingHelper.SetupTraceListeners(config, includeConsole: true);
            using var loggerFactory = LoggingHelper.CreateLoggerFactory(config);
            _logger = loggerFactory.CreateLogger<Program>();

            // Log startup
            LoggingHelper.LogStartup(_logger, "SatisfactoryServerSync Console", config);

            // Create sync service
            var syncService = new SatisfactorySyncService(config, loggerFactory.CreateLogger<SatisfactorySyncService>());

            // Show menu and handle user input
            await RunInteractiveMode(syncService);

            return 0;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Fatal error: {ex.Message}");
            _logger?.LogCritical(ex, "Fatal error in console application");
            return 1;
        }
        finally
        {
            _logger?.LogInformation("Console application ended");
            Trace.Close();
        }
    }

    private static async Task RunInteractiveMode(SatisfactorySyncService syncService)
    {
        bool running = true;
        
        while (running)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("=== Menu ===");
            System.Console.WriteLine("1. Run single sync");
            System.Console.WriteLine("2. Show configuration");
            System.Console.WriteLine("3. Test Azure connection");
            System.Console.WriteLine("4. Check game process");
            System.Console.WriteLine("5. Run continuous sync (Ctrl+C to stop)");
            System.Console.WriteLine("6. Exit");
            System.Console.WriteLine("7. Force upload (push) a specific save file to cloud");
            System.Console.Write("Select option: ");

            var input = System.Console.ReadLine();
            System.Console.WriteLine();

            switch (input)
            {
                case "1":
                    await RunSingleSync(syncService);
                    break;
                case "2":
                    ShowConfiguration();
                    break;
                case "3":
                    await TestAzureConnection(syncService);
                    break;
                case "4":
                    CheckGameProcess();
                    break;
                case "5":
                    await RunContinuousSync(syncService);
                    break;
                case "6":
                    running = false;
                    break;
                case "7":
                    await ForceUploadSaveFile(syncService);
                    break;
                default:
                    System.Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private static async Task RunSingleSync(SatisfactorySyncService syncService)
    {
        System.Console.WriteLine("Running single synchronization...");
        
        try
        {
            var result = await syncService.SynchronizeAsync();
            
            System.Console.WriteLine($"Sync completed:");
            System.Console.WriteLine($"  Action: {result.Action}");
            System.Console.WriteLine($"  Message: {result.Message}");
            
            _logger?.LogInformation("Manual sync completed: {Action} - {Message}", result.Action, result.Message);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Sync failed: {ex.Message}");
            _logger?.LogError(ex, "Manual sync failed");
        }
    }

    private static void ShowConfiguration()
    {
        System.Console.WriteLine("Current Configuration:");
        System.Console.WriteLine($"  Config file: {Path.Combine(AppContext.BaseDirectory, "config.json")}");
        
        try
        {
            var config = ConfigurationHelper.LoadConfiguration();
            System.Console.WriteLine($"  Azure Container: {config.AzureStorage.ContainerName}");
            System.Console.WriteLine($"  Process Name: {config.SatisfactoryGame.ProcessName}");
            System.Console.WriteLine($"  Save File Directory: {config.SatisfactoryGame.SaveFileDirectory}");
            System.Console.WriteLine($"  Save File Prefix: {config.SatisfactoryGame.SaveFilePrefix}");
            System.Console.WriteLine($"  Check Interval: {config.Synchronization.CheckIntervalMinutes} minutes");
            System.Console.WriteLine($"  Log File: {config.Logging.LogFilePath}");
            System.Console.WriteLine($"  Log Level: {config.Logging.LogLevel}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  Error loading config: {ex.Message}");
        }
    }

    private static async Task TestAzureConnection(SatisfactorySyncService syncService)
    {
        System.Console.WriteLine("Testing Azure Storage connection...");
        
        try
        {
            // Try to perform a sync to test the connection
            var result = await syncService.SynchronizeAsync();
            System.Console.WriteLine("Azure connection test passed!");
            System.Console.WriteLine($"Test result: {result.Action} - {result.Message}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Azure connection test failed: {ex.Message}");
            _logger?.LogError(ex, "Azure connection test failed");
        }
    }

    private static void CheckGameProcess()
    {
        System.Console.WriteLine("Checking for Satisfactory game process...");
        
        try
        {
            var config = ConfigurationHelper.LoadConfiguration();
            var processes = Process.GetProcessesByName(config.SatisfactoryGame.ProcessName);
            
            if (processes.Length > 0)
            {
                System.Console.WriteLine($"Found {processes.Length} Satisfactory process(es) running:");
                foreach (var process in processes)
                {
                    System.Console.WriteLine($"  PID: {process.Id}, Start Time: {process.StartTime}");
                }
            }
            else
            {
                System.Console.WriteLine("No Satisfactory processes found.");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error checking process: {ex.Message}");
            _logger?.LogError(ex, "Error checking game process");
        }
    }

    private static async Task RunContinuousSync(SatisfactorySyncService syncService)
    {
        System.Console.WriteLine("Starting continuous sync mode. Press Ctrl+C to stop...");
        System.Console.WriteLine();

        var config = ConfigurationHelper.LoadConfiguration();
        var interval = TimeSpan.FromMinutes(config.Synchronization.CheckIntervalMinutes);
        
        var cts = new CancellationTokenSource();
        System.Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                System.Console.Write($"[{timestamp}] Running sync... ");

                try
                {
                    var result = await syncService.SynchronizeAsync(cts.Token);
                    System.Console.WriteLine($"{result.Action} - {result.Message}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error: {ex.Message}");
                    _logger?.LogError(ex, "Error in continuous sync");
                }

                try
                {
                    await Task.Delay(interval, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Continuous sync stopped.");
        }
    }

    private static async Task ForceUploadSaveFile(SatisfactorySyncService syncService)
    {
        System.Console.Write("Enter the full path to the save file to upload: ");
        var filePath = System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            System.Console.WriteLine("File does not exist or path is invalid.");
            return;
        }
        try
        {
            System.Console.WriteLine("Calculating hash and uploading file...");
            var hash = await syncService.CalculateFileHashAsync(filePath);
            await syncService.UploadSaveFileAsync(filePath, hash, CancellationToken.None);
            // Do NOT update the local cached version of the file so we can trigger a download
            System.Console.WriteLine($"Force upload complete. Hash: {hash}");
            _logger?.LogInformation("Force uploaded {File} with hash {Hash}", filePath, hash);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Force upload failed: {ex.Message}");
            _logger?.LogError(ex, "Force upload failed");
        }
    }

    private static SyncConfiguration LoadConfiguration(string[] args)
    {
        string? configPath = null;
        
        // Check for config path in command line arguments
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--config" || args[i] == "-c")
            {
                configPath = args[i + 1];
                break;
            }
        }

        try
        {
            return ConfigurationHelper.LoadConfiguration(configPath);
        }
        catch (FileNotFoundException)
        {
            System.Console.WriteLine("Configuration file not found. Creating sample configuration...");
            
            var samplePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            ConfigurationHelper.CreateSampleConfiguration(samplePath);
            
            System.Console.WriteLine($"Sample configuration created at: {samplePath}");
            System.Console.WriteLine("Please edit the configuration file with your Azure Storage details and try again.");
            
            Environment.Exit(1);
            return null!; // Never reached
        }
    }
}
