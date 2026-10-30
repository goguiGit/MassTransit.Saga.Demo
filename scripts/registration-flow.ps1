<#
.SYNOPSIS
Runs the whole registration saga against a running Registration.Api and Notifications.Worker.

.DESCRIPTION
Registers a user, reads the verification code from the worker's /emails, sends a wrong code,
sends the right code and shows the status after each step. With -Expire it skips the confirmation
and waits for the saga deadline instead, so you can watch the registration being discarded.
Exit code 0 when every step matched the expected outcome, 1 otherwise.

.EXAMPLE
.\scripts\registration-flow.ps1
.\scripts\registration-flow.ps1 -Email luis@example.com
.\scripts\registration-flow.ps1 -Expire -ConfirmationWindowSeconds 120
#>
[CmdletBinding()]
param(
    [string] $Api = 'http://localhost:5100',
    [string] $Worker = 'http://localhost:5200',
    [string] $Email = "ana+$(Get-Date -Format 'HHmmss')@example.com",
    [switch] $Expire,
    [int] $ConfirmationWindowSeconds = 120,
    [int] $SettleSeconds = 3
)

$ErrorActionPreference = 'Stop'
$failures = 0

function Step([string] $Title) { Write-Host "`n== $Title" -ForegroundColor Cyan }

# Works on Windows PowerShell 5.1 and pwsh 7: 4xx responses are caught instead of relying on -SkipHttpErrorCheck.
function Invoke-Api([string] $Method, [string] $Url, $Body) {
    $params = @{ Method = $Method; Uri = $Url; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ($Body | ConvertTo-Json -Compress)
    }
    try {
        $response = Invoke-WebRequest @params
        $status = [int] $response.StatusCode
        $raw = $response.Content
    }
    catch {
        # WebException on 5.1, HttpResponseException on 7; the latter type does not exist on 5.1, so no typed catch.
        $response = $_.Exception.Response
        if ($null -eq $response) { throw }
        $status = [int] $response.StatusCode
        $raw = if ($response -is [System.Net.HttpWebResponse]) {
            (New-Object System.IO.StreamReader($response.GetResponseStream())).ReadToEnd()
        } else {
            $_.ErrorDetails.Message
        }
    }
    $content = if ($raw) { try { $raw | ConvertFrom-Json } catch { $raw } } else { $null }
    [pscustomobject]@{ Status = $status; Body = $content }
}

function Expect([string] $What, $Actual, $Expected) {
    if ("$Actual" -eq "$Expected") {
        Write-Host "   ok   $What = $Actual" -ForegroundColor Green
    }
    else {
        Write-Host "   FAIL $What = $Actual (expected $Expected)" -ForegroundColor Red
        $script:failures++
    }
}

function Test-Service([string] $Name, [string] $Url, [string] $Project) {
    try { Invoke-RestMethod $Url -UseBasicParsing | Out-Null; $true }
    catch {
        Write-Host "$Name not reachable at $Url. Start it with: dotnet run --project $Project" -ForegroundColor Red
        $false
    }
}

$apiUp = Test-Service 'Registration.Api' "$Api/openapi/v1.json" 'src/Registration.Api'
$workerUp = Test-Service 'Notifications.Worker' "$Worker/emails" 'src/Notifications.Worker'
if (-not ($apiUp -and $workerUp)) {
    Write-Host 'The flow needs both: the worker is what records the verification code the script reads.' -ForegroundColor Yellow
    exit 1
}

Step "Register $Email"
$registered = Invoke-Api POST "$Api/registrations" @{ email = $Email }
Expect 'status' $registered.Status 201
$userId = $registered.Body
Write-Host "   userId = $userId"
Start-Sleep -Seconds $SettleSeconds

Step 'Status while the saga waits'
$status = Invoke-Api GET "$Api/registrations/$userId"
Expect 'userStatus' $status.Body.userStatus 'PendingConfirmation'
Expect 'sagaState' $status.Body.sagaState 'AwaitingConfirmation'

Step 'Verification email recorded by the worker'
$mail = (Invoke-RestMethod "$Worker/emails") | Where-Object { $_.to -eq $Email -and $_.subject -eq 'Confirma tu email' } | Select-Object -Last 1
$code = if ($mail) { ($mail.body -replace '\D', '') } else { $null }
Expect 'code found' ($null -ne $code -and $code.Length -eq 6) $true
Write-Host "   code = $code"

Step 'Invalid email is rejected before publishing'
Expect 'status' (Invoke-Api POST "$Api/registrations" @{ email = 'not-an-email' }).Status 400

Step 'Malformed code is rejected before publishing'
Expect 'status' (Invoke-Api POST "$Api/registrations/$userId/confirm" @{ code = '12ab' }).Status 400

Step 'Unknown user'
Expect 'status' (Invoke-Api POST "$Api/registrations/$([guid]::NewGuid())/confirm" @{ code = '123456' }).Status 404

if ($Expire) {
    Step "Waiting $ConfirmationWindowSeconds s for the confirmation window to elapse"
    Start-Sleep -Seconds ($ConfirmationWindowSeconds + $SettleSeconds)
    $after = Invoke-Api GET "$Api/registrations/$userId"
    Expect 'status after expiry (user discarded)' $after.Status 404
    $welcome = (Invoke-RestMethod "$Worker/emails") | Where-Object { $_.to -eq $Email -and $_.subject -eq 'Bienvenida' }
    Expect 'welcome email sent' ($null -ne $welcome) $false
}
else {
    Step 'Wrong code: accepted by the API, ignored by the saga'
    Expect 'status' (Invoke-Api POST "$Api/registrations/$userId/confirm" @{ code = '000000' }).Status 200
    Start-Sleep -Seconds $SettleSeconds
    Expect 'sagaState still' (Invoke-Api GET "$Api/registrations/$userId").Body.sagaState 'AwaitingConfirmation'

    Step 'Right code'
    Expect 'status' (Invoke-Api POST "$Api/registrations/$userId/confirm" @{ code = $code }).Status 200
    Start-Sleep -Seconds $SettleSeconds
    $final = Invoke-Api GET "$Api/registrations/$userId"
    Expect 'userStatus' $final.Body.userStatus 'Active'
    Expect 'sagaState (saga finished)' $final.Body.sagaState ''
    $welcome = (Invoke-RestMethod "$Worker/emails") | Where-Object { $_.to -eq $Email -and $_.subject -eq 'Bienvenida' }
    Expect 'welcome email sent' ($null -ne $welcome) $true

    Step 'Confirming again'
    Expect 'status' (Invoke-Api POST "$Api/registrations/$userId/confirm" @{ code = $code }).Status 409
}

Write-Host ''
if ($failures -eq 0) { Write-Host "All steps matched." -ForegroundColor Green; exit 0 }
Write-Host "$failures step(s) did not match." -ForegroundColor Red
exit 1
