# SatisfactoryServerSync.Core

This is the core library that contains all the business logic for synchronizing Satisfactory save files with Azure Blob Storage.

## Purpose

This library provides the essential components for:

- **Configuration Management**: Loading and validating application settings from `config.json`
- **File Synchronization**: Comparing local and cloud save files using MD5 hashing
- **Azure Storage Integration**: Uploading and downloading save files to/from Azure Blob Storage
- **Process Detection**: Checking if Satisfactory game is currently running
- **Logging**: File-based trace logging with automatic rotation

## Key Components

### `SatisfactorySyncService`
The main synchronization service that:
- Detects when the Satisfactory game is running (skips sync during gameplay)
- Compares local save file hash with cloud version
- Determines sync direction (upload vs download) based on cached cloud hash
- Handles file uploads and downloads with Azure Blob Storage
- Creates backups before overwriting local files

### `ConfigurationHelper`
Utilities for:
- Loading configuration from `config.json`
- Validating required configuration settings
- Creating sample configuration files

### `FileTraceListener`
Custom trace listener that:
- Writes logs to files with timestamps
- Automatically rotates log files when they exceed 10MB
- Maintains up to 5 historical log files

### `LoggingHelper`
Utilities for:
- Setting up trace listeners for console and file logging
- Creating logger factories
- Logging application startup/shutdown information

## Configuration Model

The library uses a strongly-typed configuration model with the following sections:

- **AzureStorage**: Connection string and container settings
- **SatisfactoryGame**: Process name, save file directory, and save file prefix
- **Synchronization**: Sync intervals and file naming
- **Logging**: Log file path and level settings

## Configuration Options

The following options are available in `config.json`:

### AzureStorage
- `ConnectionString`: Azure Blob Storage connection string (required)
- `ContainerName`: Name of the blob container (required)

### SatisfactoryGame
- `ProcessName`: Name of the Satisfactory process to monitor (e.g., `FactoryGame-Win64-Shipping` or `FactoryGameSteam`)
- `SaveFileDirectory`: Directory containing your Satisfactory save files (supports environment variables)
- `SaveFilePrefix`: Prefix for your save files (e.g., `AzureSatisfapping`)

### Synchronization
- `CheckIntervalMinutes`: How often to check for sync (integer, in minutes)
- `CloudSaveFileName`: Name to use for the save file in Azure Blob Storage (e.g., `satisfactory-save.sav`)
- `CloudHashFileName`: Name to use for the hash file in Azure Blob Storage (e.g., `cloud-save-hash.md5`)
- `LocalHashCacheFileName`: Name of the local file to cache the last downloaded cloud hash (e.g., `last-cloud-hash.txt`)

### Logging
- `LogFilePath`: Path to the log file (relative or absolute)
- `LogLevel`: Minimum log level. **Allowed values:**
  - `Trace`
  - `Debug`
  - `Information`
  - `Warning`
  - `Error`
  - `Critical`

> Example: To enable debug logging, use `"LogLevel": "Debug"` (not `Debugging`).

## Dependencies

- **Azure.Storage.Blobs**: For Azure Blob Storage operations
- **Microsoft.Extensions.Configuration**: For configuration management
- **Microsoft.Extensions.Logging**: For structured logging
- **System.Security.Cryptography**: For MD5 hash calculations

## Error Handling

The library implements comprehensive error handling:
- Network issues with Azure Storage are logged but don't stop the service
- File access problems are logged with appropriate warnings
- Configuration validation prevents startup with invalid settings
- Process detection failures assume game is not running (safe default)

## Security Considerations

- Uses Azure Blob Storage connection strings (consider upgrading to Managed Identity for production)
- File operations include proper error handling and cleanup
- Log files are rotated to prevent disk space issues
- Backup files are created before overwriting local saves
