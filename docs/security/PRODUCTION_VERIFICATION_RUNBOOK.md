# Production security verification runbook

Status: **BLOCKED — REQUIRES DEPLOYED DOMAIN.** No B-United production or staging domain exists
yet. The script this runbook drives (`scripts/security/Test-ProductionSecurity.ps1`) has been
written, syntax-validated, and smoke-tested end-to-end against the local Docker Compose demo
stack (`http://localhost:8080`) to confirm it runs cleanly and reports the correct result for
every check — but it has **never been run against a real production domain**, so none of the
production-specific checks below may be marked PASS. Run it the moment a domain exists, before
declaring any deployment production-ready.

## What the smoke test against localhost already proved

| Check | Local (Development) result | Why this result is correct locally |
|---|---|---|
| TLS certificate valid | FAIL | Local demo stack is plain HTTP — expected, not a bug. |
| HTTP → HTTPS redirect | FAIL (404) | No TLS listener locally to redirect to. |
| `X-Content-Type-Options: nosniff` | **PASS** | `SecurityHeadersMiddleware` applies unconditionally. |
| `X-Frame-Options: DENY` | **PASS** | Same. |
| `Referrer-Policy` set | **PASS** | Same. |
| `Permissions-Policy` set | **PASS** | Same. |
| `Strict-Transport-Security` set | FAIL | Correct — `Program.cs` only calls `UseHsts()` outside `Development`. |
| `Cross-Origin-Resource-Policy` set | **PASS** | Same, added post-DAST-scan (see `docs/security/DAST.md`). |
| CORS allows the real SPA origin | **PASS** | `Cors:AllowedOrigins` correctly allows `http://localhost:5173`. |
| CORS rejects a hostile origin | **PASS** | Confirmed no `Access-Control-Allow-Origin` for `https://evil.example`. |
| No credentialed wildcard CORS | **PASS** | No `Access-Control-Allow-Credentials: true` ever sent. |
| Swagger UI not exposed | FAIL (200) | Correct — Swagger is intentionally Development-only, and this IS Development. |
| OpenAPI document not exposed | FAIL (200) | Same. |
| Demo credentials rejected | FAIL (200 — they work) | Correct — demo credentials are meant to work in the demo environment (ADR-010); this check should FAIL locally and MUST PASS in Production. |

This is exactly the expected split: the checks that must hold in **every** environment (defensive
headers, CORS specificity) already pass; the checks that are **environment-conditional by design**
(TLS, HSTS, Swagger, demo credentials) correctly report the Development-appropriate result. The
same script, pointed at a real production domain, must show TLS/HSTS/redirect PASS and
Swagger/OpenAPI/demo-credentials FAIL-if-exposed (i.e. PASS means "not exposed").

## Running it against a real domain

```powershell
powershell -File scripts/security/Test-ProductionSecurity.ps1 `
  -ApiUrl https://api.example.com `
  -SpaUrl https://app.example.com
```

Exits non-zero if any check fails — safe to wire into a release gate once a domain exists.

## Checks NOT automated by this script (require deployment-specific manual verification)

- **Supported TLS protocol versions**: the script reports the negotiated protocol
  (`$sslStream.SslProtocol`) but does not itself assert a minimum — cross-check the reported value
  against the hosting provider's TLS configuration (TLS 1.2+ only, no TLS 1.0/1.1/SSLv3).
- **Forwarded-headers/reverse-proxy trust configuration**: `ForwardedHeaders:KnownProxies`/
  `KnownNetworks` (see `src/BuildingBlocks/Security/Proxy/ForwardedHeadersExtensions.cs`) must be
  set to the real reverse proxy's actual IP/network once the production topology is known — an
  unconfigured deployment safely ignores forwarded headers entirely (verified by
  `ForwardedHeadersExtensionsTests`) rather than trusting them, but that means the app will log
  the proxy's IP as the client IP, not the real client, until this is configured. Verify
  `ForwardedHeaders:KnownProxies` is set in the production environment configuration and that
  `X-Forwarded-For` then correctly reflects real client IPs in logs/rate-limiting.
- **`frame-ancestors` and `upgrade-insecure-requests` CSP directives**: deliberately absent from
  the SPA's `<meta>`-delivered CSP (the spec requires browsers to ignore `frame-ancestors` in a
  `<meta>` tag, and `upgrade-insecure-requests` would break local HTTP dev) — see the comment in
  `frontend/index.html`. These must be added as real HTTP response headers at whatever serves the
  built SPA (static host / reverse proxy) once that topology is known. The Api's own
  `X-Frame-Options: DENY` already covers the Api's own responses.
- **Secrets are not placeholders**: `AddIdentityJwtAuthentication` fails fast in Production if
  `Jwt__SigningKey` is the literal `.env.example` value (see `ProductionSecretSafetyTests`) — but
  this only catches that ONE specific placeholder. Manually confirm `POSTGRES_PASSWORD` and any
  other secret in the real production `.env`/secrets manager are NOT their `.env.example`
  defaults.
- **Fake providers disabled**: `VerifyNoDemoOnlyAdaptersInProduction` (P3.32) already fails the
  app at startup if `FakePaymentProvider`/`LoggingNotificationSender`/`LoggingIdentityEmailSender`
  are registered in `Production` — confirm the deployment actually sets
  `ASPNETCORE_ENVIRONMENT=Production` (not `Staging` or an unset default), since the gate is keyed
  off that value.
- **Database encryption at rest**: per ADR-009, V1 relies on the hosting provider's disk-level
  encryption as the baseline, not application-level column encryption — confirm the production
  PostgreSQL instance has disk-level encryption enabled at the provider/infrastructure level; this
  is outside what an HTTP-based script can verify.

## Relationship to the other security-program documents

- `docs/security/DAST.md` — dynamic scanning, already executed against the local demo stack.
- `docs/security/PENTEST_SCOPE.md` — the human engagement this runbook's automated checks feed
  into, not replace.
- `docs/E2E_AUDIT_RESULT.md` — the coverage matrix showing which of these controls are
  code-verified today vs. environment-blocked.
