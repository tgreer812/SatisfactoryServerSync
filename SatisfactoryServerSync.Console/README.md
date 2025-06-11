# SatisfactoryServerSync.Console

This is a console application for debugging and testing the Satisfactory save file synchronization functionality.

## Purpose

This console application provides an interactive interface for:

- **Manual Testing**: Run single synchronization operations to test functionality
- **Configuration Validation**: View and validate current configuration settings
- **Azure Connection Testing**: Verify Azure Storage connectivity
- **Process Monitoring**: Check if Satisfactory game is currently running
- **Continuous Sync**: Run the synchronization service continuously for testing
- **Debugging**: Detailed console output and logging for troubleshooting

## Features

### Interactive Menu
The application provides a simple menu-driven interface:

1. **Run single sync** - Performs one synchronization operation
2. **Show configuration** - Displays current configuration settings
3. **Test Azure connection** - Verifies connectivity to Azure Storage
4. **Check game process** - Shows if Satisfactory is running
5. **Run continuous sync** - Starts continuous synchronization (Ctrl+C to stop)
6. **Exit** - Closes the application

### Logging
- Outputs to both console and log file simultaneously
- Shows detailed sync results and error messages
- Includes timestamps and log levels
- Logs startup and shutdown events

### Configuration
- Automatically looks for `config.json` in the application directory
- Can specify alternate config file with `--config` or `-c` parameter
- Creates sample configuration if config file is missing
- Validates configuration on startup

## Usage

### Basic Usage
```bash
SatisfactoryServerSync.Console.exe
```

### With Custom Config
```bash
SatisfactoryServerSync.Console.exe --config "C:\path\to\config.json"
```

### Command Line Options
- `--config <path>` or `-c <path>`: Specify path to configuration file

## Configuration File

The application requires a `config.json` file in the same directory as the executable. If the file doesn't exist, a sample will be created automatically.

Sample configuration structure:
```json
{
  "AzureStorage": {
    "ConnectionString": "your-azure-storage-connection-string",
    "ContainerName": "satisfactory-saves"
  },
  "SatisfactoryGame": {
    "ProcessName": "FactoryGame-Win64-Shipping",
    "SaveFileDirectory": "%LOCALAPPDATA%\\FactoryGame\\Saved\\SaveGames\\...",
    "SaveFilePrefix": "Session1_autosave_0"
  },
  "Synchronization": {
    "CheckIntervalMinutes": 1,
    "CloudSaveFileName": "satisfactory-save.sav",
    "CloudHashFileName": "cloud-save-hash.md5",
    "LocalHashCacheFileName": "last-cloud-hash.txt"
  },
  "Logging": {
    "LogFilePath": "logs\\sync.log",
    "LogLevel": "Information"
  }
}
```

## Development and Debugging

This console application is ideal for:

### Development Testing
- Test changes to synchronization logic
- Verify Azure Storage operations
- Debug configuration issues
- Test error handling scenarios

### Manual Synchronization
- Force immediate sync operations
- Test sync behavior when game is running/stopped
- Verify file upload/download operations

### Configuration Validation
- Test different configuration settings
- Verify environment variable expansion
- Check Azure connection strings

### Process Monitoring
- Monitor Satisfactory game process detection
- Test sync skipping when game is running
- Debug process name configuration

## Error Handling

The application handles various error scenarios:

- **Missing Configuration**: Creates sample config and exits gracefully
- **Invalid Configuration**: Shows validation errors with specific details
- **Azure Connection Issues**: Displays connection test results
- **File Access Problems**: Shows file permission or path issues
- **Process Detection Errors**: Handles cases where process checking fails

## Logging Output

Console output includes:
- Startup banner and configuration summary
- Menu options and user selections
- Sync operation results with timestamps
- Error messages with details
- Shutdown notifications

Log file contains:
- All console output plus additional details
- Structured logging with levels (Info, Warning, Error, etc.)
- Stack traces for exceptions
- Azure Storage operation details

## Notes

- The application requires the same configuration as the Windows service
- All sync operations are identical to those performed by the service
- Continuous sync mode simulates the service behavior
- Console output is in addition to file logging, not instead of it
- The application gracefully handles Ctrl+C interruption
