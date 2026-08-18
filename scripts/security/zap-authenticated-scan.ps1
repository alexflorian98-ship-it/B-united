<#
.SYNOPSIS
    Runs an OWASP ZAP active scan against the local Docker Compose demo Api WITH an authenticated
    session, extending docs/security/DAST.md's unauthenticated run to cover routes that require a
    logged-in user (most of the real business-logic surface -- content, progress, billing,
    questionnaires, events, chat, admin).

.DESCRIPTION
    1. Logs in as the seeded local demo client (src/Migrations/Seed/DemoAccountSeeder.cs) via a
       real POST /api/v1/auth/login call against the running Api container.
    2. Runs `zap-api-scan.py` against the Api's own OpenAPI document, using ZAP's built-in
       "Replacer" add-on (`-z -config replacer...`) to inject `Authorization: Bearer <token>` on
       every outgoing request ZAP makes -- the documented way to add auth context to a headless
       zap-*-scan.py run without writing a custom ZAP authentication script.
    3. Prints only ZAP's console PASS/WARN/FAIL summary. Deliberately does NOT write `-r`/`-J`
       report files: ZAP's HTML/JSON reports echo each request's headers (including
       Authorization) into the report body, and per DEVELOPMENT_INSTRUCTIONS.md §6 tokens must
       never be persisted into logged/archived output. The console summary contains only
       rule names, URLs, and pass/warn/fail counts -- never the token.
    5. Revokes the token's refresh-token family afterward is out of scope here (the login used to
       obtain the access token is a normal demo-account login; the access token itself is
       short-lived and never persisted to disk).
    6. Runs an additional, separate live IDOR/authorization smoke check (not a ZAP scan rule --
       ZAP's generic active-scan ruleset has no notion of "this resource belongs to a different
       user", so it cannot detect this class of bug on its own). Using the two already-seeded
       local demo identities (client + expert, no new accounts are created):
         a. Logs in as the demo client, lists their own invoices, and takes the one seeded
            invoice's id (DemoAccountSeeder always seeds exactly one paid invoice for the demo
            client's purchase).
         b. Logs in as the demo expert (a distinct user with no ownership or explicit grant over
            that invoice) and requests GET /api/v1/billing/my-invoices/{that invoiceId}. Per
            BillingController's ownership-scoped handler, a non-owned invoice id must 404 (the
            "never confirm existence to a non-owner" contract) -- any 200 here is a real IDOR.
         c. Requests GET /api/v1/admin/dashboard as the demo client (who holds no admin
            permission) and asserts 403 -- a real role/permission-boundary check.
       This is a narrow, deterministic complement to the code-level cross-user test suites
       (BillingCrossUserAccessTests, QuestionnaireCrossUserAccessTests, etc., which remain the
       primary, broader proof of ownership isolation) -- it exists to prove the *live*, currently
       running deployment enforces the same boundaries the unit-level tests assert against an
       in-memory host, not to replace that test coverage.

.PARAMETER ApiBaseUrl
    Base URL of the running Api container as seen from the ZAP container on the same Docker
    network. Defaults to the Docker Compose demo stack's service name.

.PARAMETER DockerNetwork
    Docker network shared by the Api container and the ZAP container. Defaults to the Compose
    stack's default network name.

.PARAMETER ClientEmail / ClientPassword
    Local demo credentials only (see src/Migrations/Seed/DemoAccountSeeder.cs) -- never real
    secrets. Overridable via parameters/environment for a different local seed, but no new
    credentials are invented here.

.EXAMPLE
    pwsh scripts/security/zap-authenticated-scan.ps1
#>
param(
    [string]$ApiBaseUrl = "http://bunited-api:8080",
    [string]$LocalApiBaseUrl = "http://localhost:8080",
    [string]$DockerNetwork = "b-united_default",
    [string]$ClientEmail = "demo.client@bunited.local",
    [string]$ClientPassword = "DemoAccount123!",
    [string]$ExpertEmail = "demo.expert@bunited.local",
    [string]$ExpertPassword = "DemoAccount123!"
)

$ErrorActionPreference = "Stop"

Write-Host "Logging in as the local demo client to obtain an access token for ZAP's Replacer add-on..."
$loginBody = @{ email = $ClientEmail; password = $ClientPassword } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "$LocalApiBaseUrl/api/v1/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
$accessToken = $loginResponse.accessToken
if (-not $accessToken) {
    Write-Error "Login did not return an accessToken -- cannot run an authenticated scan."
    exit 1
}
Write-Host "Obtained a short-lived access token (never printed, never written to a file)."

# ZAP's Replacer add-on rewrites every outgoing request; here it adds/overwrites the Authorization
# header on every request ZAP itself issues during the scan, so authenticated routes are actually
# exercised instead of returning 401 immediately.
$replacerConfig = @(
    "-config replacer.full_list(0).description=auth"
    "-config replacer.full_list(0).enabled=true"
    "-config replacer.full_list(0).matchtype=REQ_HEADER"
    "-config replacer.full_list(0).matchstr=Authorization"
    "-config replacer.full_list(0).regex=false"
    "-config replacer.full_list(0).replacement=Bearer $accessToken"
) -join " "

Write-Host "Running authenticated zap-api-scan.py against $ApiBaseUrl/openapi/v1.json ..."
$dockerArgs = @(
    "run", "--rm", "--network", $DockerNetwork, "-t", "ghcr.io/zaproxy/zaproxy:stable",
    "zap-api-scan.py", "-t", "$ApiBaseUrl/openapi/v1.json", "-f", "openapi", "-I", "-z", $replacerConfig
)
& docker @dockerArgs

# Note: intentionally not persisting the access token or the replacer config (which embeds it) to
# any file on disk. The console output above is the only artifact this script produces.

Write-Host ""
Write-Host "--- Live IDOR / authorization-boundary smoke check (not a ZAP rule) ---"
Write-Host "Fetching the demo client's own seeded invoice id..."
$clientInvoices = Invoke-RestMethod -Uri "$LocalApiBaseUrl/api/v1/billing/my-invoices" -Method Get `
    -Headers @{ Authorization = "Bearer $accessToken" }
$clientInvoiceId = ($clientInvoices | Select-Object -First 1).id
if (-not $clientInvoiceId) {
    Write-Warning "Demo client has no seeded invoice -- skipping the IDOR check (nothing to attempt cross-user access to)."
} else {
    Write-Host "Logging in as the demo expert (a distinct, non-owning user) to attempt cross-user access..."
    $expertLoginBody = @{ email = $ExpertEmail; password = $ExpertPassword } | ConvertTo-Json
    $expertLoginResponse = Invoke-RestMethod -Uri "$LocalApiBaseUrl/api/v1/auth/login" -Method Post -ContentType "application/json" -Body $expertLoginBody
    $expertAccessToken = $expertLoginResponse.accessToken

    $idorStatus = $null
    try {
        Invoke-WebRequest -Uri "$LocalApiBaseUrl/api/v1/billing/my-invoices/$clientInvoiceId" -Method Get `
            -Headers @{ Authorization = "Bearer $expertAccessToken" } | Out-Null
        $idorStatus = 200
    } catch {
        $idorStatus = [int]$_.Exception.Response.StatusCode
    }

    if ($idorStatus -eq 404) {
        Write-Host "PASS: a non-owning user requesting another user's invoice by id got 404 (ownership boundary enforced)."
    } else {
        Write-Error "FAIL (IDOR): expected 404 for a non-owning user's invoice request, got $idorStatus."
        exit 1
    }

    $adminStatus = $null
    try {
        Invoke-WebRequest -Uri "$LocalApiBaseUrl/api/v1/admin/dashboard" -Method Get `
            -Headers @{ Authorization = "Bearer $accessToken" } | Out-Null
        $adminStatus = 200
    } catch {
        $adminStatus = [int]$_.Exception.Response.StatusCode
    }

    if ($adminStatus -eq 403) {
        Write-Host "PASS: the demo client (no admin permission) requesting an admin endpoint got 403 (role/permission boundary enforced)."
    } else {
        Write-Error "FAIL (authorization boundary): expected 403 for a non-admin user on an admin endpoint, got $adminStatus."
        exit 1
    }
}
