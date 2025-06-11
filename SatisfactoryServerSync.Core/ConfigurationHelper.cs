using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using System.Text.Json;

namespace SatisfactoryServerSync.Core;

/// <summary>
/// Helper class for loading and managing configuration
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Loads configuration from config.json file in the application directory
    /// </summary>
    /// <param name="configPath">Optional path to config file. If null, uses config.json in app directory</param>
    /// <returns>Loaded configuration</returns>
    public static SyncConfiguration LoadConfiguration(string? configPath = null)
    {
        configPath ??= Path.Combine(AppContext.BaseDirectory, "config.json");

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var builder = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false);

        var configuration = builder.Build();
        var syncConfig = new SyncConfiguration();
        configuration.Bind(syncConfig);

        ValidateConfiguration(syncConfig);
        return syncConfig;
    }

    /// <summary>
    /// Validates that required configuration values are present
    /// </summary>
    private static void ValidateConfiguration(SyncConfiguration config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.AzureStorage.ConnectionString))
            errors.Add("AzureStorage.ConnectionString is required");

        if (string.IsNullOrWhiteSpace(config.AzureStorage.ContainerName))
            errors.Add("AzureStorage.ContainerName is required");

        if (string.IsNullOrWhiteSpace(config.SatisfactoryGame.ProcessName))
            errors.Add("SatisfactoryGame.ProcessName is required");

        if (string.IsNullOrWhiteSpace(config.SatisfactoryGame.SaveFileDirectory))
            errors.Add("SatisfactoryGame.SaveFileDirectory is required");

        if (string.IsNullOrWhiteSpace(config.SatisfactoryGame.SaveFilePrefix))
            errors.Add("SatisfactoryGame.SaveFilePrefix is required");

        if (string.IsNullOrWhiteSpace(config.Synchronization.CloudSaveFileName))
            errors.Add("Synchronization.CloudSaveFileName is required");

        if (string.IsNullOrWhiteSpace(config.Synchronization.CloudHashFileName))
            errors.Add("Synchronization.CloudHashFileName is required");

        if (string.IsNullOrWhiteSpace(config.Synchronization.LocalHashCacheFileName))
            errors.Add("Synchronization.LocalHashCacheFileName is required");

        if (config.Synchronization.CheckIntervalMinutes <= 0)
            errors.Add("Synchronization.CheckIntervalMinutes must be greater than 0");

        if (errors.Any())
        {
            throw new InvalidOperationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
        }
    }

    /// <summary>
    /// Creates a sample configuration file
    /// </summary>
    /// <param name="filePath">Path where to create the sample config</param>
    public static void CreateSampleConfiguration(string filePath)
    {
        var sampleConfig = new SyncConfiguration
        {
            AzureStorage = new AzureStorageSettings
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=your-storage-account;AccountKey=your-key;EndpointSuffix=core.windows.net",
                ContainerName = "satisfactory-saves"
            },
            SatisfactoryGame = new SatisfactoryGameSettings
            {
                ProcessName = "FactoryGame-Win64-Shipping",
                SaveFileDirectory = "%LOCALAPPDATA%\\FactoryGame\\Saved\\SaveGames\\76561198000000000",
                SaveFilePrefix = "Session1_autosave_0"
            },
            Synchronization = new SynchronizationSettings
            {
                CheckIntervalMinutes = 1,
                CloudSaveFileName = "satisfactory-save.sav",
                CloudHashFileName = "cloud-save-hash.md5",
                LocalHashCacheFileName = "last-cloud-hash.txt"
            },
            Logging = new LoggingSettings
            {
                LogFilePath = "logs\\sync.log",
                LogLevel = "Information"
            }
        };

        var json = JsonSerializer.Serialize(sampleConfig, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        File.WriteAllText(filePath, json);
    }
}
