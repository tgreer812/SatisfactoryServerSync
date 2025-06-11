using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SatisfactoryServerSync.Core;

/// <summary>
/// Configuration model for the application
/// </summary>
public class SyncConfiguration
{
    public AzureStorageSettings AzureStorage { get; set; } = new();
    public SatisfactoryGameSettings SatisfactoryGame { get; set; } = new();
    public SynchronizationSettings Synchronization { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class AzureStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}

public class SatisfactoryGameSettings
{
    public string ProcessName { get; set; } = string.Empty;
    public string SaveFileDirectory { get; set; } = string.Empty;
    public string SaveFilePrefix { get; set; } = string.Empty;
}

public class SynchronizationSettings
{
    public int CheckIntervalMinutes { get; set; } = 1;
    public string CloudSaveFileName { get; set; } = string.Empty;
    public string CloudHashFileName { get; set; } = string.Empty;
    public string LocalHashCacheFileName { get; set; } = string.Empty;
}

public class LoggingSettings
{
    public string LogFilePath { get; set; } = string.Empty;
    public string LogLevel { get; set; } = "Information";
}
