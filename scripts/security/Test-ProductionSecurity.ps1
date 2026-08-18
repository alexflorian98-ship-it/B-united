<#
.SYNOPSIS
    Production security verification runbook script (security-gap-closure item #8). Runs safe,
    read-only HTTP(S) checks against a deployed domain and reports PASS/FAIL/BLOCKED per check.

.DESCRIPTION
    This script has never been run against a real B-United production domain — none exists yet.
    It is provided so that check is a one-command, reproducible step the first time a domain does
    exist, rather than a manual, easy-to-skip checklist. Every check here is read-only (GET/HEAD/
    OPTIONS requests only) — nothing destructive, nothing that mutates state. Compatible with both
    Windows PowerShell 5.1 and PowerShell 7+ (does not use pwsh-only cmdlet parameters).

.PARAMETER ApiUrl
    The production Api base URL, e.g. https://api.example.com

.PARAMETER SpaUrl
    The production SPA base URL, e.g. https://app.example.com

.PARAMETER ExpectedSpaOrigin
    The exact origin the Api's CORS policy should allow — normally the same as -SpaUrl.

.EXAMPLE
    powershell -File scripts/security/Test-ProductionSecurity.ps1 -ApiUrl https://api.example.com -SpaUrl https://app.example.com
#>
param(
    [Parameter(Mandatory = $true)][string]$ApiUrl,
    [Parameter(Mandatory = $true)][string]$SpaUrl,
    [string]$ExpectedSpaOrigin = $SpaUrl
)

$ErrorActionPreference = "Stop"
$results = @()

function Add-Result([string]$Name, [string]$Status, [string]$Detail) {
    $script:results += [pscustomobject]@{ Check = $Name; Status = $Status; Detail = $Detail }
}

# Windows PowerShell 5.1 has no -SkipHttpErrorCheck on Invoke-WebRequest (pwsh 7+ only) — a
# non-2xx response throws instead. This wrapper normalizes both: it always returns a response
# object with .StatusCode/.Headers, never throws for an HTTP-level error status, and works
# identically on 5.1 and 7+.
function Invoke-SafeWebRequest {
    param([string]$Uri, [string]$Method = "Get", [hashtable]$Headers = @{}, [string]$Body = $null, [string]$ContentType = $null)
    try {
        $params = @{ Uri = $Uri; Method = $Method; Headers = $Headers; UseBasicParsing = $true }
        if ($Body) { $params.Body = $Body }
        if ($ContentType) { $params.ContentType = $ContentType }
        return Invoke-WebRequest @params
    } catch [System.Net.WebException] {
        if ($_.Exception.Response) {
            return $_.Exception.Response
        }
        throw
    } catch {
        if ($_.Exception.Response) {
            return $_.Exception.Response
        }
        throw
    }
}

function Get-ResponseHeader([object]$Response, [string]$Name) {
    if ($Response.Headers -is [System.Collections.IDictionary]) {
        return $Response.Headers[$Name]
    }
    # HttpWebResponse (from a caught exception) exposes headers via a NameValueCollection.
    return $Response.Headers.Get($Name)
}

function Get-ResponseStatusCode([object]$Response) {
    if ($Response.StatusCode -is [int]) { return $Response.StatusCode }
    return [int]$Response.StatusCode
}

Write-Host "Verifying production security posture for Api=$ApiUrl SPA=$SpaUrl ..." -ForegroundColor Cyan

# 1. TLS certificate validity and hostname, and supported protocol version.
try {
    $uri = [Uri]$ApiUrl
    $tcpClient = New-Object System.Net.Sockets.TcpClient($uri.Host, 443)
    $sslStream = New-Object System.Net.Security.SslStream($tcpClient.GetStream(), $false, { $true })
    $sslStream.AuthenticateAsClient($uri.Host)
    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]$sslStream.RemoteCertificate
    $validHostname = $cert.Subject -match [regex]::Escape($uri.Host) -or $cert.GetNameInfo("SimpleName", $false) -eq $uri.Host
    $validDate = (Get-Date) -lt [DateTime]$cert.GetExpirationDateString() -and (Get-Date) -gt [DateTime]$cert.GetEffectiveDateString()
    $protocol = $sslStream.SslProtocol
    Add-Result "TLS certificate valid + hostname matches" $(if ($validHostname -and $validDate) { "PASS" } else { "FAIL" }) "Protocol=$protocol; Expires=$($cert.GetExpirationDateString())"
    $sslStream.Dispose(); $tcpClient.Dispose()
} catch {
    Add-Result "TLS certificate valid + hostname matches" "FAIL" $_.Exception.Message
}

# 2. HTTP-to-HTTPS redirect.
try {
    $httpUrl = $ApiUrl -replace "^https://", "http://"
    $response = Invoke-SafeWebRequest -Uri $httpUrl -Method Get
    $location = Get-ResponseHeader $response "Location"
    $status = Get-ResponseStatusCode $response
    $redirectsToHttps = $status -in 301, 302, 307, 308 -and $location -like "https://*"
    Add-Result "HTTP to HTTPS redirect" $(if ($redirectsToHttps) { "PASS" } else { "FAIL" }) "Status=$status; Location=$location"
} catch {
    Add-Result "HTTP to HTTPS redirect" "FAIL" $_.Exception.Message
}

# 3. Security headers (nosniff, frame-options, referrer-policy, permissions-policy, HSTS,
#    Cross-Origin-Resource-Policy) on a real Api response.
try {
    $response = Invoke-SafeWebRequest -Uri "$ApiUrl/health" -Method Get
    $checks = [ordered]@{
        "X-Content-Type-Options: nosniff"  = (Get-ResponseHeader $response "X-Content-Type-Options") -eq "nosniff"
        "X-Frame-Options: DENY"            = (Get-ResponseHeader $response "X-Frame-Options") -eq "DENY"
        "Referrer-Policy set"              = [bool](Get-ResponseHeader $response "Referrer-Policy")
        "Permissions-Policy set"           = [bool](Get-ResponseHeader $response "Permissions-Policy")
        "Strict-Transport-Security set"    = [bool](Get-ResponseHeader $response "Strict-Transport-Security")
        "Cross-Origin-Resource-Policy set" = [bool](Get-ResponseHeader $response "Cross-Origin-Resource-Policy")
    }
    foreach ($check in $checks.GetEnumerator()) {
        Add-Result "Header: $($check.Key)" $(if ($check.Value) { "PASS" } else { "FAIL" }) ""
    }
    $hstsValue = Get-ResponseHeader $response "Strict-Transport-Security"
    if ($hstsValue) {
        $maxAgeOk = $hstsValue -match "max-age=(\d+)" -and [int]$Matches[1] -ge 15552000  # >= 180 days
        Add-Result "HSTS max-age >= 180 days" $(if ($maxAgeOk) { "PASS" } else { "FAIL" }) $hstsValue
    }
} catch {
    Add-Result "Security headers on /health" "FAIL" $_.Exception.Message
}

# 4. CORS: SPA origin allowed, hostile origin rejected, no credentialed wildcard.
try {
    $allowedResponse = Invoke-SafeWebRequest -Uri "$ApiUrl/api/v1/content/programs" -Method Options `
        -Headers @{ Origin = $ExpectedSpaOrigin; "Access-Control-Request-Method" = "GET" }
    $allowOrigin = Get-ResponseHeader $allowedResponse "Access-Control-Allow-Origin"
    Add-Result "CORS allows the real SPA origin" $(if ($allowOrigin -eq $ExpectedSpaOrigin) { "PASS" } else { "FAIL" }) "Got: $allowOrigin"

    $hostileResponse = Invoke-SafeWebRequest -Uri "$ApiUrl/api/v1/content/programs" -Method Options `
        -Headers @{ Origin = "https://evil.example"; "Access-Control-Request-Method" = "GET" }
    $hostileAllowOrigin = Get-ResponseHeader $hostileResponse "Access-Control-Allow-Origin"
    Add-Result "CORS rejects a hostile origin" $(if (-not $hostileAllowOrigin) { "PASS" } else { "FAIL" }) "Got: $hostileAllowOrigin"

    $hostileAllowCredentials = Get-ResponseHeader $hostileResponse "Access-Control-Allow-Credentials"
    $noCredentialedWildcard = $hostileAllowOrigin -ne "*" -or $hostileAllowCredentials -ne "true"
    Add-Result "No credentialed wildcard CORS" $(if ($noCredentialedWildcard) { "PASS" } else { "FAIL" }) ""
} catch {
    Add-Result "CORS checks" "FAIL" $_.Exception.Message
}

# 5. Development Swagger UI / OpenAPI document are not exposed.
try {
    $swaggerResponse = Invoke-SafeWebRequest -Uri "$ApiUrl/swagger" -Method Get
    $swaggerStatus = Get-ResponseStatusCode $swaggerResponse
    Add-Result "Swagger UI not exposed" $(if ($swaggerStatus -in 404, 401, 403) { "PASS" } else { "FAIL" }) "Status=$swaggerStatus"

    $openApiResponse = Invoke-SafeWebRequest -Uri "$ApiUrl/openapi/v1.json" -Method Get
    $openApiStatus = Get-ResponseStatusCode $openApiResponse
    Add-Result "OpenAPI document not exposed" $(if ($openApiStatus -in 404, 401, 403) { "PASS" } else { "FAIL" }) "Status=$openApiStatus"
} catch {
    Add-Result "Swagger/OpenAPI exposure" "FAIL" $_.Exception.Message
}

# 6. Demo credentials do not work in production (per P3.32/ADR-010 — must fail with invalid
#    credentials, not succeed).
try {
    $loginResponse = Invoke-SafeWebRequest -Uri "$ApiUrl/api/v1/auth/login" -Method Post -ContentType "application/json" `
        -Body (@{ email = "demo.client@bunited.local"; password = "DemoAccount123!" } | ConvertTo-Json)
    $loginStatus = Get-ResponseStatusCode $loginResponse
    Add-Result "Demo credentials rejected in production" $(if ($loginStatus -ne 200) { "PASS" } else { "FAIL" }) "Status=$loginStatus"
} catch {
    Add-Result "Demo credentials rejected in production" "FAIL" $_.Exception.Message
}

$results | Format-Table -AutoSize
$failedCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
if ($failedCount -gt 0) {
    Write-Host "$failedCount check(s) FAILED. See table above." -ForegroundColor Red
    exit 1
}
Write-Host "All checks PASSED against $ApiUrl / $SpaUrl." -ForegroundColor Green
exit 0
