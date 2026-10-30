<#
.SYNOPSIS
Starts the whole demo: RabbitMQ (Podman), Registration.Api, Notifications.Worker and the Angular panel.

.DESCRIPTION
Each service opens in its own console window so its log stays visible. The script waits for every piece
to answer before starting the next one and prints the URLs at the end. Re-running it is safe: pieces that
already answer are left alone.

.EXAMPLE
.\scripts\demo-up.ps1
.\scripts\demo-up.ps1 -Transport Sql
.\scripts\demo-up.ps1 -SkipWorker            # watch the message wait in the EmailSender queue
.\scripts\demo-up.ps1 -SkipWeb -NoBrowser
#>
[CmdletBinding()]
param(
    [ValidateSet('RabbitMq', 'Sql', 'AzureServiceBus')]
    [string] $Transport = 'RabbitMq',
    [switch] $SkipWorker,
    [switch] $SkipWeb,
    [switch] $NoBrowser,
    [int] $TimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }

function Step([string] $Title) { Write-Host "`n== $Title" -ForegroundColor Cyan }

function Test-Url([string] $Url, [string] $Credential) {
    try {
        $params = @{ Uri = $Url; UseBasicParsing = $true; TimeoutSec = 3 }
        if ($Credential) { $params.Headers = @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($Credential)) } }
        Invoke-WebRequest @params | Out-Null
        $true
    }
    catch { $false }
}

function Wait-Url([string] $Name, [string] $Url, [string] $Credential) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Url $Url $Credential) { Write-Host "   ok   $Name at $Url" -ForegroundColor Green; return }
        Start-Sleep -Seconds 2
    }
    throw "$Name did not answer at $Url within $TimeoutSeconds s. Check its window."
}

function Start-Window([string] $Title, [string] $WorkingDirectory, [string] $Command) {
    $script = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$WorkingDirectory'; $Command"
    Start-Process $shell -ArgumentList '-NoExit', '-Command', $script | Out-Null
    Write-Host "   started '$Title' in a new window"
}

function Test-Port([int] $Port) {
    try { $client = New-Object Net.Sockets.TcpClient; $client.Connect('localhost', $Port); $client.Dispose(); $true } catch { $false }
}

function Assert-RabbitMq {
    Wait-Url 'RabbitMQ management' 'http://localhost:15672/api/overview' 'guest:guest'

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (-not (Test-Port 5672) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (-not (Test-Port 5672)) { throw 'RabbitMQ is not accepting AMQP connections on port 5672.' }
    Write-Host '   ok   AMQP listening on 5672' -ForegroundColor Green

    $plugins = (podman exec rabbitmq rabbitmq-plugins list -e -m 2>&1) -join "`n"
    if (-not $plugins.Contains('rabbitmq_delayed_message_exchange')) {
        throw "The rabbitmq_delayed_message_exchange plugin is not enabled; the saga deadline needs it. Use the masstransit/rabbitmq image or run: podman exec rabbitmq rabbitmq-plugins enable rabbitmq_delayed_message_exchange"
    }
    Write-Host '   ok   delayed message exchange plugin enabled' -ForegroundColor Green
}

if ($Transport -eq 'RabbitMq') {
    Step 'RabbitMQ (Podman)'
    if (-not (Test-Url 'http://localhost:15672/api/overview' 'guest:guest')) {
        podman machine start 2>&1 | Where-Object { $_ -notmatch 'already running' } | ForEach-Object { Write-Host "   $_" }
        $exists = (podman ps -a --format '{{.Names}}') -contains 'rabbitmq'
        if ($exists) { podman start rabbitmq | Out-Null }
        else { podman run -d --name rabbitmq -p 5672:5672 -p 15672:15672 docker.io/masstransit/rabbitmq:latest | Out-Null }
    }
    Assert-RabbitMq
}

Step "Registration.Api ($Transport)"
if (Test-Url 'http://localhost:5100/openapi/v1.json') { Write-Host '   ok   already running' -ForegroundColor Green }
else {
    Start-Window 'Registration.Api' $root "`$env:Messaging__Transport = '$Transport'; dotnet run --project src/Registration.Api"
    Wait-Url 'Registration.Api' 'http://localhost:5100/openapi/v1.json'
}

if (-not $SkipWorker) {
    Step "Notifications.Worker ($Transport)"
    if (Test-Url 'http://localhost:5200/emails') { Write-Host '   ok   already running' -ForegroundColor Green }
    else {
        Start-Window 'Notifications.Worker' $root "`$env:Messaging__Transport = '$Transport'; dotnet run --project src/Notifications.Worker"
        Wait-Url 'Notifications.Worker' 'http://localhost:5200/emails'
    }
}

if (-not $SkipWeb) {
    Step 'Angular panel'
    $web = Join-Path $root 'web/registration-ui'
    if (Test-Url 'http://localhost:4200/') { Write-Host '   ok   already running' -ForegroundColor Green }
    else {
        if (-not (Test-Path (Join-Path $web 'node_modules'))) {
            Write-Host '   npm install (first run)...'
            Push-Location $web; npm install --no-audit --no-fund | Out-Null; Pop-Location
        }
        Start-Window 'registration-ui' $web 'npm start'
        Wait-Url 'Angular panel' 'http://localhost:4200/'
    }
    if (-not $NoBrowser) { Start-Process 'http://localhost:4200/' }
}

Write-Host ''
Write-Host 'Everything is up:' -ForegroundColor Green
Write-Host '   API      http://localhost:5100/openapi/v1.json'
if (-not $SkipWorker) { Write-Host '   Worker   http://localhost:5200/emails' }
if ($Transport -eq 'RabbitMq') { Write-Host '   RabbitMQ http://localhost:15672  (guest / guest)' }
if (-not $SkipWeb) { Write-Host '   Panel    http://localhost:4200' }
Write-Host "Stop everything with scripts\demo-down.ps1"
