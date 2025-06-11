using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace SatisfactoryServerSync.Core;

/// <summary>
/// Core synchronization service that handles save file synchronization with Azure Blob Storage
/// </summary>
public class SatisfactorySyncService
{
    protected readonly SyncConfiguration _config;
    protected readonly ILogger<SatisfactorySyncService> _logger;
    protected readonly BlobServiceClient? _blobServiceClient;
    protected readonly BlobContainerClient? _containerClient;

    // Main constructor for production
    public SatisfactorySyncService(SyncConfiguration config, ILogger<SatisfactorySyncService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobServiceClient = new BlobServiceClient(config.AzureStorage.ConnectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(config.AzureStorage.ContainerName);
        
        _logger.LogInformation("SatisfactorySyncService initialized");
    }

    // Protected constructor for testing (no Azure clients)
    protected SatisfactorySyncService(SyncConfiguration config, ILogger<SatisfactorySyncService> logger, bool skipAzureInit)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobServiceClient = null;
        _containerClient = null;
    }

    /// <summary>
    /// Performs a synchronization check and action if needed
    /// </summary>
    public async Task<SyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Satisfactory game is running
            if (IsGameRunning())
            {
                _logger.LogDebug("Satisfactory game is running, skipping synchronization");
                return new SyncResult(SyncAction.Skipped, "Game is running");
            }

            // Expand environment variables in directory
            var saveDir = Environment.ExpandEnvironmentVariables(_config.SatisfactoryGame.SaveFileDirectory);
            var prefix = _config.SatisfactoryGame.SaveFilePrefix;
            if (!Directory.Exists(saveDir))
            {
                _logger.LogWarning("Save file directory not found: {SaveFileDirectory}", saveDir);
                return new SyncResult(SyncAction.None, $"Save file directory not found: {saveDir}");
            }
            // Find the most recently modified .sav file with the prefix
            var files = Directory.GetFiles(saveDir, $"{prefix}*.sav");
            if (files.Length == 0)
            {
                _logger.LogWarning("No save files found with prefix {Prefix} in {Dir}", prefix, saveDir);
                return new SyncResult(SyncAction.None, $"No save files found with prefix {prefix} in {saveDir}");
            }
            var localSaveFilePath = files
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .First().FullName;

            // Ensure container exists
            await EnsureContainerExistsAsync(cancellationToken);

            // Get local file hash
            var localHash = await CalculateFileHashAsync(localSaveFilePath);
            _logger.LogDebug("Local save file hash: {Hash}", localHash);

            // Get cloud hash from the cloud-save-hash.md5 file
            var cloudHashFromFile = await GetCloudHashFromFileAsync(cancellationToken);
            _logger.LogDebug("Cloud hash from file: {Hash}", cloudHashFromFile ?? "null");

            // Get cached cloud hash (last downloaded version)
            var cachedCloudHash = await GetCachedCloudHashAsync();
            _logger.LogDebug("Cached cloud hash: {Hash}", cachedCloudHash ?? "null");

            // Normalize and trim hash values before comparison
            var localHashNorm = localHash.Trim().ToLowerInvariant();
            var cloudHashFromFileNorm = (cloudHashFromFile ?? "").Trim().ToLowerInvariant();
            var cachedCloudHashNorm = (cachedCloudHash ?? "").Trim().ToLowerInvariant();

            // Log detailed debug information
            _logger.LogDebug("Local file selected for sync: {File}", localSaveFilePath);
            _logger.LogDebug("Local file hash: {Hash}", localHash);
            _logger.LogDebug("Cloud file name: {CloudFileName}", _config.Synchronization.CloudSaveFileName);
            _logger.LogDebug("Cloud hash from file: {Hash}", cloudHashFromFile ?? "null");
            _logger.LogDebug("Cached cloud hash: {Hash}", cachedCloudHash ?? "null");

            // Determine sync action needed
            if (cloudHashFromFile == null)
            {
                // No cloud save exists, upload local version
                await UploadSaveFileAsync(localSaveFilePath, localHashNorm, cancellationToken);
                await SaveCachedCloudHashAsync(localHashNorm);
                _logger.LogInformation("Uploaded local save file to cloud (first upload)");
                return new SyncResult(SyncAction.Upload, "First upload to cloud");
            }

            if (localHashNorm == cloudHashFromFileNorm)
            {
                // Files are in sync
                _logger.LogDebug("Local and cloud save files are in sync");
                return new SyncResult(SyncAction.None, "Files are in sync");
            }

            // Files are different, determine direction
            if (cloudHashFromFileNorm != cachedCloudHashNorm)
            {
                // Cloud version has changed since last download, download it
                await DownloadSaveFileAsync(localSaveFilePath, cancellationToken);
                await SaveCachedCloudHashAsync(cloudHashFromFileNorm);
                _logger.LogInformation("Downloaded newer cloud save file");
                return new SyncResult(SyncAction.Download, "Downloaded newer cloud version");
            }
            else
            {
                // Local version is newer, upload it
                await UploadSaveFileAsync(localSaveFilePath, localHashNorm, cancellationToken);
                await SaveCachedCloudHashAsync(localHashNorm);
                _logger.LogInformation("Uploaded newer local save file");
                return new SyncResult(SyncAction.Upload, "Uploaded newer local version");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronization");
            return new SyncResult(SyncAction.Error, ex.Message);
        }
    }

    protected virtual bool IsGameRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName(_config.SatisfactoryGame.ProcessName);
            return processes.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if game is running");
            return false; // Assume not running if we can't check
        }
    }

    public virtual async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = await md5.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    protected virtual async Task<string?> GetCloudHashFromFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(_config.Synchronization.CloudHashFileName);
            
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cloud hash from file");
            return null;
        }
    }

    protected virtual async Task<string?> GetCachedCloudHashAsync()
    {
        try
        {
            var cacheFilePath = Path.Combine(AppContext.BaseDirectory, _config.Synchronization.LocalHashCacheFileName);
            
            if (!File.Exists(cacheFilePath))
            {
                return null;
            }

            return (await File.ReadAllTextAsync(cacheFilePath)).Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading cached cloud hash");
            return null;
        }
    }

    protected virtual async Task SaveCachedCloudHashAsync(string hash)
    {
        try
        {
            var cacheFilePath = Path.Combine(AppContext.BaseDirectory, _config.Synchronization.LocalHashCacheFileName);
            await File.WriteAllTextAsync(cacheFilePath, hash.Trim().ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving cached cloud hash");
        }
    }

    public virtual async Task UploadSaveFileAsync(string localFilePath, string hash, CancellationToken cancellationToken)
    {
        if (_containerClient == null)
            throw new InvalidOperationException("Blob container client is not initialized.");

        // Upload save file
        var saveFileBlobClient = _containerClient.GetBlobClient(_config.Synchronization.CloudSaveFileName);
        using var fileStream = File.OpenRead(localFilePath);
        await saveFileBlobClient.UploadAsync(fileStream, overwrite: true, cancellationToken: cancellationToken);

        // Upload hash file
        var hashBlobClient = _containerClient.GetBlobClient(_config.Synchronization.CloudHashFileName);
        using var hashStream = new MemoryStream(Encoding.UTF8.GetBytes(hash));
        await hashBlobClient.UploadAsync(hashStream, overwrite: true, cancellationToken: cancellationToken);
    }

    protected virtual async Task DownloadSaveFileAsync(string localFilePath, CancellationToken cancellationToken)
    {
        if (_containerClient == null)
            throw new InvalidOperationException("Blob container client is not initialized.");

        var blobClient = _containerClient.GetBlobClient(_config.Synchronization.CloudSaveFileName);
        
        // Create backup of existing local file
        var backupPath = localFilePath + ".backup." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.Copy(localFilePath, backupPath);
        _logger.LogDebug("Created backup of local save file: {BackupPath}", backupPath);

        // Download cloud file
        using var fileStream = File.Create(localFilePath);
        await blobClient.DownloadToAsync(fileStream, cancellationToken);
    }

    protected virtual async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
    {
        if (_containerClient == null)
            throw new InvalidOperationException("Blob container client is not initialized.");
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Result of a synchronization operation
/// </summary>
public record SyncResult(SyncAction Action, string Message);

/// <summary>
/// Possible synchronization actions
/// </summary>
public enum SyncAction
{
    None,
    Upload,
    Download,
    Skipped,
    Error
}
