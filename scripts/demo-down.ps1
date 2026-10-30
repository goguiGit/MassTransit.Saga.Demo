<#
.SYNOPSIS
Stops what demo-up.ps1 started: the Angular dev server, Notifications.Worker and Registration.Api.
With -Rabbit it also stops the RabbitMQ container; with -Podman the Podman machine too.
#>
[CmdletBinding()]
param(
    [switch] $Rabbit,
    [switch] $Podman
)

function Stop-Port([string] $Name, [int] $Port) {
    $owners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
    if (-not $owners) { Write-Host "   $Name (port $Port): not running"; return }
    foreach ($processId in $owners) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            Stop-Process -Id $processId -Force
            Write-Host "   $Name (port $Port): stopped $($process.ProcessName) $processId" -ForegroundColor Green
        }
    }
}

Stop-Port 'Angular panel' 4200
Stop-Port 'Notifications.Worker' 5200
Stop-Port 'Registration.Api' 5100
Get-Process Registration.Api, Notifications.Worker -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if ($Rabbit -or $Podman) {
    podman stop rabbitmq 2>&1 | Out-Null
    Write-Host '   RabbitMQ container stopped' -ForegroundColor Green
}
if ($Podman) {
    podman machine stop 2>&1 | Out-Null
    Write-Host '   Podman machine stopped' -ForegroundColor Green
}
