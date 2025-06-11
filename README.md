# SatisfactoryServerSync

A .NET 8 solution that automatically synchronizes Satisfactory save files with Azure Blob Storage, allowing you and your friends to seamlessly share save files across different computers.

## Overview

This tool solves the problem of sharing Satisfactory save files between multiple players by using Azure Blob Storage as a central location. The system intelligently determines whether to upload your local save or download the cloud version based on which one is newer.

## Key Features

### 🔄 Smart Synchronization
- **Hash-based comparison**: Uses MD5 hashes to determine which save file is newer
- **Bidirectional sync**: Automatically uploads local changes or downloads cloud updates
- **Game detection**: Only synchronizes when Satisfactory is not running
- **Backup creation**: Creates backups before overwriting local files

### 🎮 Game-Aware
- **Process monitoring**: Detects when Satisfactory is running and skips sync
- **Configurable process name**: Easy to update if game executable changes
- **Session detection**: Works with different save file locations and naming

### ☁️ Azure Integration
- **Blob Storage**: Efficient file storage and retrieval
- **Connection string authentication**: Simple setup with Azure Storage account
- **Container isolation**: Keeps save files organized in dedicated containers
- **Hash optimization**: Uploads small hash files to minimize bandwidth usage

### 🔒 Reliable & Safe
- **Error handling**: Continues operation despite temporary network issues
- **Comprehensive logging**: Detailed file and console logging with rotation
- **Configuration validation**: Prevents startup with invalid settings
- **Graceful degradation**: Logs errors and continues rather than crashing

## Solution Structure

This solution consists of 4 projects:

### 📚 SatisfactoryServerSync.Core
The main library containing all business logic:
- Configuration management and validation
- Azure Blob Storage operations
- File hash calculation and comparison
- Process detection utilities
- Custom file logging with rotation

### 🧪 SatisfactoryServerSync.Tests
Unit tests for the core library:
- Configuration loading and validation tests
- File operations testing
- Error handling verification
- Logging functionality tests

### 🖥️ SatisfactoryServerSync.Console
Interactive console application for debugging and testing:
- Manual sync operations
- Configuration viewing and validation
- Azure connection testing
- Continuous sync mode for testing
- Real-time status monitoring

### 🔧 SatisfactoryServerSync.Service
Windows Service for background operation:
- Automatic startup with Windows
- Continuous background monitoring
- Windows Event Log integration
- Service management scripts
- Production-ready deployment

## Quick Start

### 1. Prerequisites
- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- Azure Storage account with Blob Storage
- Administrator privileges (for service installation)

### 2. Setup Azure Storage
1. Create an Azure Storage account
2. Create a blob container (e.g., "satisfactory-saves")
3. Copy the connection string from Azure Portal

### 3. Build the Solution
```bash
git clone <repository-url>
cd SatisfactoryServerSync
dotnet build --configuration Release
```

### 4. Configure the Application
Edit `config.json` with your settings:
```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net",
    "ContainerName": "satisfactory-saves"
  },
  "SatisfactoryGame": {
    "ProcessName": "FactoryGame-Win64-Shipping",
    "SaveFileDirectory": "%LOCALAPPDATA%\\FactoryGame\\Saved\\SaveGames\\YourSteamId",
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

### 5. Test with Console Application
```bash
cd SatisfactoryServerSync.Console\bin\Release\net8.0
SatisfactoryServerSync.Console.exe
```

### 6. Install as Windows Service
```powershell
cd SatisfactoryServerSync.Service\bin\Release\net8.0
.\Install-Service.ps1
```

## Configuration Guide

### Finding Your Save File Path
1. Open Windows Explorer
2. Navigate to: `%LOCALAPPDATA%\FactoryGame\Saved\SaveGames\`
3. Look for a folder with your Steam ID (long number)
4. Find your save files and note the prefix (e.g. `AzureSatisfapping`)
5. Use the folder as `SaveFileDirectory` and the prefix as `SaveFilePrefix` in the config

### Azure Storage Setup
1. **Create Storage Account**: In Azure Portal, create a new Storage Account
2. **Create Container**: Add a new blob container (e.g., "satisfactory-saves")
3. **Get Connection String**: Copy from "Access keys" section in Azure Portal
4. **Set Permissions**: Ensure the account has read/write access to blob storage

### Process Name Configuration
- **Default**: "FactoryGame-Win64-Shipping" (for Windows 64-bit)
- **Steam**: Usually the same, but check Task Manager when game is running
- **Epic Games**: May be different, verify the process name

## How It Works

### Synchronization Logic
1. **Check if game is running** - Skip sync if Satisfactory is active
2. **Calculate local file hash** - MD5 hash of current save file
3. **Download cloud hash** - Small text file containing cloud save hash
4. **Compare hashes** - Determine if files are in sync
5. **Check cached hash** - Last known cloud hash to determine sync direction
6. **Perform sync operation** - Upload local file or download cloud file
7. **Update cache** - Store new cloud hash for next comparison

### Sync Direction Logic
- **Local == Cloud**: No sync needed
- **Cloud hash changed**: Download cloud version (someone else updated)
- **Local changed, cloud unchanged**: Upload local version (you made changes)
- **No cloud version**: Upload local version (first sync)

### File Management
- **Hash files**: Small text files containing MD5 hashes for quick comparison
- **Backup creation**: Local files are backed up before being overwritten
- **Cache files**: Local cache tracks last known cloud state
- **Log rotation**: Log files are automatically rotated to prevent disk space issues

## Troubleshooting

### Common Issues

#### Sync Not Working
1. **Check configuration**: Verify Azure connection string and save file path
2. **Test connectivity**: Use console app to test Azure connection
3. **Check permissions**: Ensure file access permissions
4. **Review logs**: Check log files for specific error messages

#### Service Won't Start
1. **Check Event Log**: Look for startup errors in Windows Event Log
2. **Verify config**: Ensure config.json exists and is valid
3. **Test manually**: Try running console version first
4. **Check dependencies**: Ensure .NET 8.0 Runtime is installed

#### Game Detection Issues
1. **Verify process name**: Check Task Manager for correct process name
2. **Test detection**: Use console app to test game process detection
3. **Update config**: Modify ProcessName if game executable changed

### Debugging Tools

#### Console Application
Use the console app for interactive debugging:
- Run single sync operations
- Test Azure connectivity
- Monitor real-time sync status
- View detailed error messages

#### Log Analysis
Check log files for detailed information:
- Sync operation results
- Error messages and stack traces
- Azure Storage operation details
- Startup and shutdown events

#### Windows Event Log
For service-related issues:
```powershell
Get-EventLog -LogName Application -Source SatisfactoryServerSync -Newest 20
```

## Security Best Practices

### Azure Storage Security
- **Rotate keys regularly**: Update connection strings periodically
- **Use minimal permissions**: Only grant necessary blob storage access
- **Consider Private Endpoints**: For enhanced network security
- **Monitor access**: Review Azure Storage logs for unusual activity

### Local Security
- **Secure config files**: Protect config.json with appropriate file permissions
- **Service account**: Consider dedicated service account instead of LocalSystem
- **Backup encryption**: Consider encrypting backup files
- **Network security**: Ensure firewall allows HTTPS outbound connections

## Contributing

### Development Setup
1. Clone the repository
2. Open in Visual Studio or VS Code
3. Build solution: `dotnet build`
4. Run tests: `dotnet test`
5. Test with console app for development

### Project Structure
- **Core**: Business logic and utilities
- **Tests**: Unit tests for core functionality  
- **Console**: Interactive testing and debugging
- **Service**: Production Windows Service

### Coding Standards
- Follow C# naming conventions
- Include XML documentation for public APIs
- Add unit tests for new functionality
- Use structured logging with proper log levels
- Handle errors gracefully with appropriate logging

## License

[Specify your license here]

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review log files for error details
3. Test with the console application first
4. Open an issue with detailed information

## Roadmap

Future enhancements could include:
- **Multiple save file support**: Sync multiple save files simultaneously
- **Conflict resolution**: Handle simultaneous updates from multiple players
- **Cloud providers**: Support for other cloud storage providers
- **GUI application**: Windows Forms or WPF interface
- **Notification system**: Desktop notifications for sync events
- **Managed Identity**: Enhanced Azure authentication options
