using Microsoft.Extensions.Logging;
using Moq;
using SatisfactoryServerSync.Core;

namespace SatisfactoryServerSync.Tests;

public class ConfigurationHelperTests
{
    [Fact]
    public void LoadConfiguration_WithValidConfig_ReturnsConfiguration()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var configPath = Path.Combine(tempDir, $"test_config_{Guid.NewGuid()}.json");
        
        try
        {
            ConfigurationHelper.CreateSampleConfiguration(configPath);

            // Act
            var config = ConfigurationHelper.LoadConfiguration(configPath);

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.AzureStorage);
            Assert.NotNull(config.SatisfactoryGame);
            Assert.NotNull(config.Synchronization);
            Assert.NotNull(config.Logging);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }

    [Fact]
    public void LoadConfiguration_WithMissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigurationHelper.LoadConfiguration(nonExistentPath));
    }

    [Fact]
    public void CreateSampleConfiguration_CreatesValidConfigFile()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var configPath = Path.Combine(tempDir, $"sample_config_{Guid.NewGuid()}.json");

        try
        {
            // Act
            ConfigurationHelper.CreateSampleConfiguration(configPath);

            // Assert
            Assert.True(File.Exists(configPath));
            var content = File.ReadAllText(configPath);
            Assert.Contains("AzureStorage", content);
            Assert.Contains("SatisfactoryGame", content);
            Assert.Contains("Synchronization", content);
            Assert.Contains("Logging", content);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }
}

public class FileTraceListenerTests
{
    [Fact]
    public void Constructor_CreatesLogDirectory()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var logDir = Path.Combine(tempDir, $"test_logs_{Guid.NewGuid()}");
        var logPath = Path.Combine(logDir, "test.log");

        try
        {
            // Act
            using var listener = new FileTraceListener(logPath);

            // Assert
            Assert.True(Directory.Exists(logDir));
        }
        finally
        {
            if (Directory.Exists(logDir))
                Directory.Delete(logDir, true);
        }
    }

    [Fact]
    public void WriteLine_WritesToLogFile()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var logPath = Path.Combine(tempDir, $"test_log_{Guid.NewGuid()}.log");
        var testMessage = "Test log message";

        try
        {
            // Act
            using (var listener = new FileTraceListener(logPath))
            {
                listener.WriteLine(testMessage);
            }

            // Assert
            Assert.True(File.Exists(logPath));
            var content = File.ReadAllText(logPath);
            Assert.Contains(testMessage, content);
        }
        finally
        {
            if (File.Exists(logPath))
                File.Delete(logPath);
        }
    }
}

public class SyncConfigurationTests
{
    [Fact]
    public void SyncConfiguration_DefaultConstructor_InitializesProperties()
    {
        // Act
        var config = new SyncConfiguration();

        // Assert
        Assert.NotNull(config.AzureStorage);
        Assert.NotNull(config.SatisfactoryGame);
        Assert.NotNull(config.Synchronization);
        Assert.NotNull(config.Logging);
    }

    [Fact]
    public void SynchronizationSettings_DefaultCheckInterval_IsOne()
    {
        // Act
        var settings = new SynchronizationSettings();

        // Assert
        Assert.Equal(1, settings.CheckIntervalMinutes);
    }
}