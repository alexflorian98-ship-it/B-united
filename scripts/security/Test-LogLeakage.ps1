<#
.SYNOPSIS
    Security-gap-closure item #5: proves the running Api never writes passwords, bearer tokens,
    or refresh tokens into its logs, using unique synthetic canary values sent through real HTTP
    requests against a live instance.

.DESCRIPTION
    Sends requests carrying unique, never-otherwise-used canary strings in the password, the
    Authorization header, and a refresh-token field, then inspects the container's actual
    Serilog output (`docker logs`) for those exact strings. Every canary is entirely synthetic —
    this never touches or prints a real secret. Exits non-zero if any canary leaked, so it can be
    wired into CI as a gate.

.PARAMETER ApiBaseUrl
    Base URL of the running Api, without the /api/v1 suffix. Defaults to the local Docker Compose
    demo stack.

.PARAMETER ContainerName
    Docker container name to read logs from. Defaults to the Docker Compose demo stack's Api
    container.

.EXAMPLE
    pwsh scripts/security/Test-LogLeakage.ps1
#>
param(
    [string]$ApiBaseUrl = "http://localhost:8080",
    [string]$ContainerName = "bunited-api"
)

$ErrorActionPreference = "Stop"
$canarySuffix = [guid]::NewGuid().ToString("N").Substring(0, 12)
$passwordCanary = "CanaryPwd_$canarySuffix!Aa1"
$bearerCanary = "canary-bearer-token-$canarySuffix"
$refreshCanary = "canary-refresh-token-$canarySuffix"

Write-Host "Sending canary requests against $ApiBaseUrl ..."

# 1. Wrong-password login attempt — the canary password must never be logged, on success or
#    (as here) failure.
try {
    Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/auth/login" -Method Post -ContentType "application/json" `
        -Body (@{ email = "log-leakage-canary@example.invalid"; password = $passwordCanary } | ConvertTo-Json) `
        -ErrorAction SilentlyContinue | Out-Null
} catch { }

# 2. A bogus bearer token on a protected endpoint — the token value itself must never be logged,
#    even though the request is rejected.
try {
    Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/profile" -Method Get `
        -Headers @{ Authorization = "Bearer $bearerCanary" } -ErrorAction SilentlyContinue | Out-Null
} catch { }

# 3. A bogus refresh token — same requirement.
try {
    Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/auth/refresh" -Method Post -ContentType "application/json" `
        -Body (@{ refreshToken = $refreshCanary } | ConvertTo-Json) -ErrorAction SilentlyContinue | Out-Null
} catch { }

Start-Sleep -Seconds 1

$logs = docker logs $ContainerName 2>&1 | Out-String

$leaks = @()
if ($logs -like "*$passwordCanary*") { $leaks += "password" }
if ($logs -like "*$bearerCanary*") { $leaks += "bearer token" }
if ($logs -like "*$refreshCanary*") { $leaks += "refresh token" }

if ($leaks.Count -gt 0) {
    Write-Error "LOG LEAKAGE CONFIRMED: found $($leaks -join ', ') in container logs. See DEVELOPMENT_INSTRUCTIONS.md §6."
    exit 1
}

Write-Host "PASS: no password, bearer token, or refresh token canary found in $ContainerName logs."
exit 0
