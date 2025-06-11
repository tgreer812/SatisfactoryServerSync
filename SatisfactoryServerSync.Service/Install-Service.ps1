# Install-Service.ps1
# PowerShell script to install SatisfactoryServerSync as a Windows Service

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "SatisfactoryServerSync",
    
    [Parameter(Mandatory=$false)]
    [string]$DisplayName = "Satisfactory Server Sync",
    
    [Parameter(Mandatory=$false)]
    [string]$Description = "Synchronizes Satisfactory save files with Azure Blob Storage",
    
    [Parameter(Mandatory=$false)]
    [string]$BinaryPath,
    
    [Parameter(Mandatory=$false)]
    [string]$StartupType = "Automatic"
)

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Right-click PowerShell and select 'Run as Administrator'."
    exit 1
}

# If no binary path specified, use the current directory
if (-not $BinaryPath) {
    $BinaryPath = Join-Path $PSScriptRoot "SatisfactoryServerSync.Service.exe"
}

# Check if binary exists
if (-not (Test-Path $BinaryPath)) {
    Write-Error "Service binary not found at: $BinaryPath"
    Write-Host "Please ensure you've built the project and the executable exists."
    exit 1
}

# Check if config file exists
$configPath = Join-Path (Split-Path $BinaryPath) "config.json"
if (-not (Test-Path $configPath)) {
    Write-Warning "Configuration file not found at: $configPath"
    Write-Host "A sample configuration will be created when the service first runs."
    Write-Host "You will need to edit it with your Azure Storage details before the service will work properly."
}

Write-Host "Installing SatisfactoryServerSync Windows Service..."
Write-Host "Service Name: $ServiceName"
Write-Host "Display Name: $DisplayName"
Write-Host "Binary Path: $BinaryPath"
Write-Host "Startup Type: $StartupType"
Write-Host ""

try {
    # Check if service already exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Service '$ServiceName' already exists. Stopping and removing..."
        
        # Stop service if running
        if ($existingService.Status -eq 'Running') {
            Stop-Service -Name $ServiceName -Force
            Write-Host "Service stopped."
        }
        
        # Remove existing service
        sc.exe delete $ServiceName
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Existing service removed."
        } else {
            Write-Error "Failed to remove existing service. Exit code: $LASTEXITCODE"
            exit 1
        }
        
        # Wait a moment for the service to be fully removed
        Start-Sleep -Seconds 2
    }
    
    # Create the service
    $result = New-Service -Name $ServiceName -BinaryPathName $BinaryPath -DisplayName $DisplayName -Description $Description -StartupType $StartupType
    
    if ($result) {
        Write-Host "Service installed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "To start the service:"
        Write-Host "  Start-Service -Name $ServiceName"
        Write-Host ""
        Write-Host "To check service status:"
        Write-Host "  Get-Service -Name $ServiceName"
        Write-Host ""
        Write-Host "To view service logs:"
        Write-Host "  Get-EventLog -LogName Application -Source SatisfactoryServerSync -Newest 50"
        Write-Host ""
        Write-Host "Configuration file location:"
        Write-Host "  $configPath"
        Write-Host ""
        
        # Offer to start the service
        $startNow = Read-Host "Do you want to start the service now? (y/N)"
        if ($startNow -eq 'y' -or $startNow -eq 'Y') {
            Start-Service -Name $ServiceName
            $status = Get-Service -Name $ServiceName
            Write-Host "Service started. Status: $($status.Status)" -ForegroundColor Green
        }
    }
}
catch {
    Write-Error "Failed to install service: $($_.Exception.Message)"
    exit 1
}
