# SatisfactoryServerSync.Service

This is a Windows Service that runs continuously in the background to synchronize Satisfactory save files with Azure Blob Storage.

## Purpose

This Windows Service provides:

- **Continuous Monitoring**: Runs in the background checking for sync opportunities
- **Automatic Startup**: Starts automatically when Windows boots (configurable)
- **Game Detection**: Only synchronizes when Satisfactory is not running
- **Error Recovery**: Continues running despite temporary network or storage issues
- **Windows Integration**: Integrates with Windows Service Manager and Event Log

## Features

### Background Operation
- Runs as a Windows Service without user interaction
- Starts automatically on system boot (if configured)
- Continues running even when user logs off
- Minimal resource usage during operation

### Intelligent Synchronization
- Checks for sync opportunities every minute (configurable)
- Skips sync when Satisfactory game is running
- Compares file hashes to determine sync direction
- Creates backups before overwriting local files

### Robust Error Handling
- Logs errors to Windows Event Log and file
- Continues operation despite temporary failures
- Automatic retry for transient network issues
- Graceful shutdown on service stop

### Comprehensive Logging
- Detailed file logging with automatic rotation
- Windows Event Log integration
- Startup and shutdown logging
- Sync operation results and errors

## Installation

### Prerequisites
1. Windows 10/11 or Windows Server 2016+
2. .NET 8.0 Runtime installed
3. Administrator privileges for installation
4. Azure Storage account with blob storage

### Installation Steps

#### Method 1: Using PowerShell Script (Recommended)
1. Build the project:
   ```bash
   dotnet build --configuration Release
   ```

2. Navigate to the output directory:
   ```bash
   cd bin\Release\net8.0\
   ```

3. Run the installation script as Administrator:
   ```powershell
   .\Install-Service.ps1
   ```

#### Method 2: Manual Installation
1. Open Command Prompt as Administrator

2. Navigate to the service executable directory

3. Install the service:
   ```cmd
   sc create SatisfactoryServerSync binPath="C:\path\to\SatisfactoryServerSync.Service.exe" start=auto
   ```

4. Set service description:
   ```cmd
   sc description SatisfactoryServerSync "Synchronizes Satisfactory save files with Azure Blob Storage"
   ```

### Configuration

1. **Edit config.json** - The service requires a `config.json` file in the same directory as the executable:

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

2. **Configure Azure Storage**:
   - Create an Azure Storage account
   - Create a blob container (e.g., "satisfactory-saves")
   - Copy the connection string to the config file

3. **Find your save file path**:
   - Typically located at: `%LOCALAPPDATA%\FactoryGame\Saved\SaveGames\[SteamId]`
   - Look for save files with a common prefix (e.g. `AzureSatisfapping`)
   - Use the folder as `SaveFileDirectory` and the prefix as `SaveFilePrefix` in the config

## Service Management

### Starting the Service
```powershell
Start-Service -Name SatisfactoryServerSync
```

### Stopping the Service
```powershell
Stop-Service -Name SatisfactoryServerSync
```

### Checking Service Status
```powershell
Get-Service -Name SatisfactoryServerSync
```

### Service Configuration
```powershell
# Set to start automatically (default)
Set-Service -Name SatisfactoryServerSync -StartupType Automatic

# Set to start manually
Set-Service -Name SatisfactoryServerSync -StartupType Manual

# Disable the service
Set-Service -Name SatisfactoryServerSync -StartupType Disabled
```

## Uninstallation

### Method 1: Using PowerShell Script (Recommended)
```powershell
.\Uninstall-Service.ps1
```

### Method 2: Manual Uninstallation
1. Stop the service:
   ```powershell
   Stop-Service -Name SatisfactoryServerSync
   ```

2. Remove the service:
   ```cmd
   sc delete SatisfactoryServerSync
   ```

## Monitoring and Troubleshooting

### Viewing Logs

#### File Logs
- Default location: `logs\sync.log` (relative to service executable)
- Automatic rotation when files exceed 10MB
- Keeps up to 5 historical log files

#### Windows Event Log
```powershell
Get-EventLog -LogName Application -Source SatisfactoryServerSync -Newest 50
```

### Common Issues

#### Service Won't Start
1. Check Windows Event Log for error details
2. Verify config.json exists and is valid
3. Ensure Azure Storage connection string is correct
4. Check file permissions on service directory

#### Synchronization Not Working
1. Verify Satisfactory save file path is correct
2. Check Azure Storage connectivity
3. Ensure container exists in Azure Storage
4. Review sync logs for specific error messages

#### High CPU/Memory Usage
1. Check log level - set to "Warning" or "Error" for production
2. Increase check interval if sync is too frequent
3. Verify no infinite retry loops in logs

### Performance Tuning

#### Reduce Resource Usage
- Set `LogLevel` to "Warning" or "Error" in production
- Increase `CheckIntervalMinutes` for less frequent checks
- Ensure proper Azure Storage region selection

#### Improve Sync Speed
- Use Azure Storage in same region as your location
- Ensure stable internet connection
- Check for antivirus interference with file access

## Security Considerations

### Azure Storage Security
- Use Azure Storage connection strings (rotate keys regularly)
- Consider upgrading to Managed Identity for production deployments
- Restrict Azure Storage access to necessary operations only

### Local Security
- Service runs under LocalSystem account by default
- Ensure config.json has appropriate file permissions
- Consider running under dedicated service account for enhanced security

### Network Security
- Service communicates with Azure Storage over HTTPS
- No inbound network connections required
- Firewall should allow outbound HTTPS (port 443)

## Advanced Configuration

### Custom Service Account
To run the service under a specific user account:

1. Create a dedicated service account
2. Grant "Log on as a service" right
3. Modify service configuration:
   ```cmd
   sc config SatisfactoryServerSync obj="DOMAIN\ServiceAccount" password="password"
   ```

### Multiple Instances
To run multiple instances for different save files:

1. Create separate directories for each instance
2. Use different service names and display names
3. Configure separate config.json files
4. Install each instance separately

### Monitoring Integration
The service can be monitored using:
- Windows Performance Counters
- System Center Operations Manager (SCOM)
- Third-party monitoring tools via Windows Event Log
- PowerShell scripts checking service status

## Troubleshooting Scripts

### Check Service Status Script
```powershell
# CheckServiceStatus.ps1
$service = Get-Service -Name SatisfactoryServerSync -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Service Status: $($service.Status)"
    Write-Host "Startup Type: $($service.StartType)"
} else {
    Write-Host "Service not installed"
}
```

### View Recent Logs Script
```powershell
# ViewRecentLogs.ps1
Get-EventLog -LogName Application -Source SatisfactoryServerSync -Newest 10 | 
    Format-Table TimeGenerated, EntryType, Message -AutoSize
```
