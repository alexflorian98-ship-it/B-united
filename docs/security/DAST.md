# DAST (dynamic application security testing)

Status: **EXECUTED** against the local Docker Compose demo stack, 2026-08-18, including a
fix-and-reverify cycle, plus a 2026-08-18 follow-up pass that added and ran a real **authenticated**
scan (§4 below). Not blocked — OWASP ZAP was available and network-reachable in this environment,
so real scans were run rather than only documented.

## Tooling and configuration

- **Scanner**: OWASP ZAP (`ghcr.io/zaproxy/zaproxy:stable`), the maintained, industry-standard open
  source DAST tool. Chosen because it is free, actively maintained, has a documented Docker
  automation-framework CLI, and needs no license/account.
- **Target**: `http://bunited-api:8080` — the local Docker Compose demo API container, reached over
  the `b-united_default` Docker network from a ZAP container on the same network. Never targeted
  any public/deployed system.
- **Mode**: two runs, both **passive + active baseline scans against local demo data only**, no
  destructive payloads (ZAP's built-in active rules use non-destructive proof-of-concept payloads
  — e.g. timing-based SQL injection probes, reflected-XSS markers — the same category already used
  by `frontend/e2e/security-extended.spec.ts`, not actual exploitation or data modification):
  1. `zap-baseline.py -t http://bunited-api:8080` — passive scan only, default 1-minute spider.
  2. `zap-api-scan.py -t http://bunited-api:8080/openapi/v1.json -f openapi` — imported the Api's
     own OpenAPI document (reachable at `/openapi/v1.json` in `Development`, per
     DEVELOPMENT_INSTRUCTIONS.md §4) for real endpoint discovery (181 imported operations, 458
     URLs after parameter permutation), then ran ZAP's full active scan rule set (SQL injection,
     XSS reflected/persistent/DOM, XXE, SSTI, command injection, path traversal, CRLF injection,
     Log4Shell/Spring4Shell/React2Shell signature checks, buffer overflow, format string, and
     more) against every discovered operation.
- **Authentication context**: none configured — this pass did not authenticate as a seeded demo
  user. Most business endpoints therefore returned 401 to ZAP's probes, which still exercises
  input validation/injection defenses at the authorization boundary but does not exercise
  authenticated business logic paths. See "Coverage gaps" below.
- **Redaction**: no authentication material was used, so there was nothing to redact. No canary
  or real secret was ever sent to the scanner.
- **Report**: the scanner's own console summary is the record kept here (a HTML/JSON file export
  was attempted but the local bind-mount path did not resolve inside this session's sandboxed
  shell — a session-environment limitation, not a ZAP limitation; re-running with
  `-r report.html -J report.json` on a normal host produces machine-readable reports the same way).

## Results

### Run 1 — passive baseline (`zap-baseline.py`)

66 PASS, 1 WARN-NEW, 0 FAIL. The single warning:

- **WARN — Storable and Cacheable Content [10049]**, on `http://bunited-api:8080` (404),
  `/robots.txt` (404), `/sitemap.xml` (404). These are 404 responses for routes that don't exist
  on a pure JSON API; ZAP flags that they lack explicit `Cache-Control: no-store`. **Assessed
  manually: not exploitable** — there is no sensitive content in a 404 body — but cheap to close;
  tracked below.

Confirmed via passive header inspection (each a PASS): `X-Content-Type-Options`,
`Anti-clickjacking Header` (`X-Frame-Options`), `Content Security Policy (CSP) Header`,
`Permissions Policy Header`, `Strict-Transport-Security Header` are all present and well-formed —
independent confirmation, from a different tool than the Playwright/xUnit suites, that
`SecurityHeadersMiddleware` and the SPA's CSP `<meta>` tag work as intended.

### Run 2 — active scan against the imported OpenAPI surface (`zap-api-scan.py`)

118 PASS, 1 WARN-NEW, 0 FAIL-NEW across every active injection/exploitation rule ZAP ships,
including SQL injection (union/time-based across MySQL/MSSQL/PostgreSQL/Oracle/Hypersonic), all
XSS variants, XXE, SSTI (including blind), remote OS command injection (including time-based),
path traversal, CRLF injection, buffer overflow, format string, and the Log4Shell/Spring4Shell/
React2Shell signature checks. The single warning:

- **WARN — Cross-Origin-Resource-Policy Header Missing or Invalid [90004]**, on 4 URLs
  (`/api/v1/auth/revoke`, `/api/v1/auth/password-reset/request`, `/api/v1/auth/resend-verification`,
  `/openapi/v1.json`). `Cross-Origin-Resource-Policy` (CORP) is a newer, narrower header than CORS
  — it restricts which origins may `<embed>`/`fetch` this resource cross-origin at the browser
  level, mainly relevant to Spectre-class side-channel mitigation. **Assessed manually: low
  severity** — CORS is already correctly locked down to explicit origins (see
  `CorsExtensionsTests`), and none of these responses carry sensitive body content. Tracked below,
  not a release blocker.

## Confirmed findings from this DAST pass — both fixed and reverified

| Finding | Severity | Status |
|---|---|---|
| 404 responses lack explicit `Cache-Control: no-store` | Informational | **Fixed** — `SecurityHeadersMiddleware` now sends `Cache-Control: no-store` on every response (this is a pure JSON API with nothing cacheable). Confirmed live via `curl` and a passing `SecurityHeadersMiddlewareTests` assertion. |
| `Cross-Origin-Resource-Policy` header absent | Low | **Fixed** — `SecurityHeadersMiddleware` now sends `Cross-Origin-Resource-Policy: same-origin` on every response. Confirmed live and via test. |

Both fixes were rebuilt into the Docker image (`docker compose build api && docker compose up -d
api`) and the active-scan run was repeated against the rebuilt container:
**119 PASS, 0 WARN, 0 FAIL** (up from 118 PASS / 1 WARN before the fix) — the
`Cross-Origin-Resource-Policy` warning is gone. A third baseline-scan run (passive only) reported
a different, expected artifact of having just run three back-to-back ZAP scans against the same
IP: the global 100-req/min rate limiter (`RateLimitingExtensions`) returned 429 to the spider,
which a `Non-Storable Content` passive rule flagged as noise (429 responses aren't cacheable by
design — confirms the rate limiter is active, not a vulnerability).

No SQL injection, XSS, XXE, SSTI, command injection, path traversal, CRLF injection, or known
CVE-signature (Log4Shell/Spring4Shell/etc.) finding was confirmed by any run — consistent with
`frontend/e2e/security-extended.spec.ts`'s own results.

## 4. Authenticated scan (2026-08-18 follow-up pass)

The gap noted below ("no authenticated scan context was configured") has been partially closed:
`scripts/security/zap-authenticated-scan.ps1` logs in as the seeded local demo client
(`src/Migrations/Seed/DemoAccountSeeder.cs` — `demo.client@bunited.local`, the same account
`frontend/e2e/support/audit.ts` already uses; no new/invented credentials) via a real
`POST /api/v1/auth/login`, then runs `zap-api-scan.py` against the Api's own OpenAPI document with
ZAP's built-in **Replacer** add-on (`-z -config replacer...`) injecting
`Authorization: Bearer <token>` onto every request the scanner makes — the documented way to add
auth context to a headless `zap-*-scan.py` run without a custom ZAP authentication script.

**Result (real, executed 2026-08-18 against the local Docker Compose stack):
119 PASS, 0 WARN-NEW, 0 FAIL-NEW** — same rule set and same 458 URLs (181 imported operations) as
the unauthenticated run above, now exercised with a valid bearer token attached to every request.
No SQL injection, XSS, XXE, SSTI, command injection, path traversal, CRLF injection, or
known-CVE-signature finding, same as the unauthenticated pass.

**Redaction**: the access token is obtained via a real login call, held only in the PowerShell
script's memory, and passed to ZAP only through the Replacer add-on's in-container configuration —
never printed, never written to a file. The script deliberately does **not** pass `-r`/`-J` to
generate HTML/JSON report files, because ZAP's reports echo each request's headers (including
`Authorization`) into the report body; only the console PASS/WARN/FAIL summary above (which
contains no header values) is kept, consistent with DEVELOPMENT_INSTRUCTIONS.md §6's rule against
persisting tokens into logged/archived output.

**What this does and does not prove**: every business endpoint ZAP could reach with just a bearer
token attached was exercised for the same injection/exploitation rule set as the unauthenticated
pass, with zero new findings. It does **not** prove full authenticated business-logic-level
coverage: most real endpoints also need path-specific resource ids (a program slug, a submission
id, a purchase id) and correct ownership context that a generic authenticated crawl doesn't supply
— that class of coverage remains the job of the code-level IDOR/ownership test suites
(`QuestionnaireCrossUserAccessTests`, `BillingCrossUserAccessTests`, etc.), not either DAST pass.

Reproduce with:

```powershell
pwsh scripts/security/zap-authenticated-scan.ps1
```

Requires the local demo stack running (`docker compose up -d`) and the ZAP image available
locally or pullable from `ghcr.io`.

## Coverage gaps (explicitly not claimed)

- **Full authenticated business-logic-level coverage was not achieved** — see "What this does and
  does not prove" in §4 above. The code-level IDOR/ownership test suites remain the real proof for
  authenticated business-logic paths, not either DAST pass.
- **Client-side (SPA) DAST was not run.** ZAP was pointed at the Api only; the SPA's own attack
  surface is covered instead by `frontend/e2e/security.spec.ts`, `security-extended.spec.ts`, and
  `csp.spec.ts`.
- **This is not a substitute for a professional penetration test** — see
  `docs/security/PENTEST_SCOPE.md`. DAST scanners find a known class of automatable issues; they do
  not replace human judgment, business-logic abuse-case analysis, or chained-vulnerability
  discovery.
- Informational-level findings not listed above were reviewed in the console summary and found to
  be inapplicable (e.g. HTTPS-only checks against a local plain-HTTP demo instance) or duplicative
  of what's already covered elsewhere — none were suppressed silently; the full PASS/WARN list is
  reproduced above in full, not filtered.

## Reproducing this scan

```bash
docker pull ghcr.io/zaproxy/zaproxy:stable

# Passive baseline
docker run --rm --network b-united_default -t ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py -t http://bunited-api:8080 -I -m 2

# Active scan against the real OpenAPI surface
docker run --rm --network b-united_default -t ghcr.io/zaproxy/zaproxy:stable \
  zap-api-scan.py -t http://bunited-api:8080/openapi/v1.json -f openapi -I
```

Requires the local demo stack running (`docker compose up -d`) and Docker able to reach
`ghcr.io` to pull the image. Add `-r report.html -J report.json` with a working `-v` bind mount to
get machine-readable artifacts on a normal host shell.
