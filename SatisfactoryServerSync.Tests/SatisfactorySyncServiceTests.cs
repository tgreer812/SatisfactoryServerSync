using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SatisfactoryServerSync.Core;
using Xunit;

namespace SatisfactoryServerSync.Tests;

public class SatisfactorySyncServiceTests
{
    private SyncConfiguration GetDefaultConfig(string tempDir)
    {
        return new SyncConfiguration
        {
            AzureStorage = new AzureStorageSettings
            {
                ConnectionString = "UseDevelopmentStorage=true;",
                ContainerName = "test-container"
            },
            SatisfactoryGame = new SatisfactoryGameSettings
            {
                ProcessName = "FakeGame",
                SaveFileDirectory = tempDir,
                SaveFilePrefix = "TestSave"
            },
            Synchronization = new SynchronizationSettings
            {
                CheckIntervalMinutes = 1,
                CloudSaveFileName = "cloudsave.sav",
                CloudHashFileName = "cloudsave.md5",
                LocalHashCacheFileName = "last-cloud-hash.txt"
            },
            Logging = new LoggingSettings
            {
                LogFilePath = Path.Combine(tempDir, "test.log"),
                LogLevel = "Debug"
            }
        };
    }

    [Fact]
    public async Task SynchronizeAsync_WhenGameIsRunning_ReturnsSkipped()
    {
        // Arrange
        var config = GetDefaultConfig(Path.GetTempPath());
        var logger = Mock.Of<ILogger<SatisfactorySyncService>>();
        var service = new TestableSyncService(config, logger)
        {
            GameRunning = true
        };

        // Act
        var result = await service.SynchronizeAsync();

        // Assert
        Assert.Equal(SyncAction.Skipped, result.Action);
        Assert.Contains("Game is running", result.Message);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenCloudIsNewer_OverwritesLocal()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var config = GetDefaultConfig(tempDir);
        var logger = Mock.Of<ILogger<SatisfactorySyncService>>();
        var service = new TestableSyncService(config, logger)
        {
            GameRunning = false,
            LocalHash = "oldhash",
            CloudHash = "newhash",
            LastDownloadedCloudHash = "oldhash",
            CloudIsNewer = true
        };
        var localFile = Path.Combine(tempDir, "TestSave_autosave_1.sav");
        File.WriteAllText(localFile, "old content");

        // Act
        var result = await service.SynchronizeAsync();

        // Assert
        if (result.Action == SyncAction.Error)
        {
            throw new Exception($"Sync returned error: {result.Message}. File exists: {File.Exists(localFile)}");
        }
        Assert.Equal(SyncAction.Download, result.Action);
        Assert.True(service.Downloaded);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenLocalIsNewer_UploadsToCloud()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var config = GetDefaultConfig(tempDir);
        var logger = Mock.Of<ILogger<SatisfactorySyncService>>();
        var service = new TestableSyncService(config, logger)
        {
            GameRunning = false,
            LocalHash = "newhash",
            CloudHash = "oldhash",
            LastDownloadedCloudHash = "oldhash",
            CloudIsNewer = false
        };
        var localFile = Path.Combine(tempDir, "TestSave_autosave_1.sav");
        File.WriteAllText(localFile, "new content");

        // Act
        var result = await service.SynchronizeAsync();

        // Assert
        Assert.Equal(SyncAction.Upload, result.Action);
        Assert.True(service.Uploaded);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenHashesMatch_NoActionTaken()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var config = GetDefaultConfig(tempDir);
        var logger = Mock.Of<ILogger<SatisfactorySyncService>>();
        var service = new TestableSyncService(config, logger)
        {
            GameRunning = false,
            LocalHash = "samehash",
            CloudHash = "samehash",
            LastDownloadedCloudHash = "samehash",
            CloudIsNewer = false
        };
        var localFile = Path.Combine(tempDir, "TestSave_autosave_1.sav");
        File.WriteAllText(localFile, "same content");

        // Act
        var result = await service.SynchronizeAsync();

        // Assert
        Assert.Equal(SyncAction.None, result.Action);
        Assert.False(service.Uploaded);
        Assert.False(service.Downloaded);
        Directory.Delete(tempDir, true);
    }

    // Helper class to override file/process/blob logic
    private class TestableSyncService : SatisfactorySyncService
    {
        public bool GameRunning { get; set; }
        public string LocalHash { get; set; } = "";
        public string CloudHash { get; set; } = "";
        public string LastDownloadedCloudHash { get; set; } = "";
        public bool CloudIsNewer { get; set; }
        public bool Uploaded { get; private set; }
        public bool Downloaded { get; private set; }

        public TestableSyncService(SyncConfiguration config, ILogger<SatisfactorySyncService> logger)
            : base(config, logger, true) { }

        protected override bool IsGameRunning() => GameRunning;
        public override Task<string> CalculateFileHashAsync(string filePath) => Task.FromResult(LocalHash);
        protected override Task<string?> GetCloudHashFromFileAsync(CancellationToken ct) => Task.FromResult<string?>(CloudHash);
        protected override Task<string?> GetCachedCloudHashAsync() => Task.FromResult<string?>(LastDownloadedCloudHash);
        protected override Task SaveCachedCloudHashAsync(string hash) { LastDownloadedCloudHash = hash; return Task.CompletedTask; }
        public override Task UploadSaveFileAsync(string localFilePath, string hash, CancellationToken ct) { Uploaded = true; return Task.CompletedTask; }
        protected override Task DownloadSaveFileAsync(string localFilePath, CancellationToken ct) { Downloaded = true; return Task.CompletedTask; }
        protected override Task EnsureContainerExistsAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
