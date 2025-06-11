# Uninstall-Service.ps1
# PowerShell script to uninstall SatisfactoryServerSync Windows Service

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "SatisfactoryServerSync"
)

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Right-click PowerShell and select 'Run as Administrator'."
    exit 1
}

Write-Host "Uninstalling SatisfactoryServerSync Windows Service..."
Write-Host "Service Name: $ServiceName"
Write-Host ""

try {
    # Check if service exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $existingService) {
        Write-Warning "Service '$ServiceName' not found. It may already be uninstalled."
        exit 0
    }
    
    Write-Host "Found service: $($existingService.DisplayName)"
    Write-Host "Current status: $($existingService.Status)"
    Write-Host ""
    
    # Stop service if running
    if ($existingService.Status -eq 'Running') {
        Write-Host "Stopping service..."
        Stop-Service -Name $ServiceName -Force
        
        # Wait for service to stop
        $timeout = 30 # seconds
        $elapsed = 0
        do {
            Start-Sleep -Seconds 1
            $elapsed++
            $currentService = Get-Service -Name $ServiceName
        } while ($currentService.Status -eq 'Running' -and $elapsed -lt $timeout)
        
        if ($currentService.Status -eq 'Running') {
            Write-Warning "Service did not stop within $timeout seconds. Forcing removal..."
        } else {
            Write-Host "Service stopped successfully."
        }
    }
    
    # Remove the service
    Write-Host "Removing service..."
    sc.exe delete $ServiceName
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service uninstalled successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Note: Log files and configuration files are not automatically removed."
        Write-Host "You may want to manually clean up:"
        Write-Host "  - Configuration file: config.json"
        Write-Host "  - Log files in the logs directory"
        Write-Host "  - Cached hash file: last-cloud-hash.txt"
    } else {
        Write-Error "Failed to remove service. Exit code: $LASTEXITCODE"
        Write-Host "You may need to restart your computer and try again."
        exit 1
    }
}
catch {
    Write-Error "Failed to uninstall service: $($_.Exception.Message)"
    exit 1
}
