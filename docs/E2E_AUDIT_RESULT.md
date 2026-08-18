# B-United E2E audit result

Date: 2026-08-18 (security-program expansion pass, following the same-day 100/100 automated E2E audit)
Automated-controls decision: **PASS**
Production-security-readiness decision: **NOT READY — multiple controls BLOCKED pending a real deployed domain and an external penetration test**

> **All implemented automated controls passed. This is not a complete security certification.**
> A 100/100 automated score means every control that can be verified without a real production
> domain, a real payment provider, or an authorized external tester has been implemented and
> passed. It does not mean the product has been penetration-tested, does not mean production TLS/
> CORS/HSTS have been verified against a real domain (none exists), and does not mean the Stripe
> integration is secure (it doesn't exist yet — V1 uses `FakePaymentProvider`, ADR-010). See the
> coverage matrix below for exactly which of those remain BLOCKED or NOT APPLICABLE, and why.

## 1. Release decision

- **Automated controls: PASS.** Every control in this repository that can be automated today
  passed — backend build/tests, frontend build/tests/locale-parity, the full Playwright suite,
  and a real DAST scan.
- **Production-security readiness: NOT READY.** Section 8 (TLS/HSTS/CORS in a real deployment),
  section 3's real Stripe webhook security, and section 10's external penetration test are all
  genuinely BLOCKED on things that don't exist yet (a deployed domain, a real payment provider, an
  authorized tester) — not on unwritten code or untested logic.

## 2. Automated scores

| Area | Score | Evidence |
| --- | ---: | --- |
| UI/UX | 100/100 | 64/64 checks (32 desktop + 32 mobile), from a real canonical (all-projects, single-invocation) run — see §6/§7a. |
| Security | 100/100 | 13/13 Playwright security checks (CSP + abuse), all passing in the same canonical run. |
| Flow | 100/100 | 2/2 flow checks, same canonical run. |
| Weighted total | 100/100 | `frontend/e2e-results/score.md` — now labels run type (canonical/focused), timestamp, commit SHA, and executed projects on every run (§7a). |
| Playwright canonical run | **17/17, exit 0, no hang** | `npx playwright test`, single invocation, all 3 projects (§6/§7a). Was 16/17 before the 2026-08-18 follow-up fix. |
| Backend unit/integration tests | 447/447 | `dotnet test BUnited.sln`, 0 failures, 14 test projects (§6/§7a — up from 432/432, `Notifications.Tests` went from 0 to 15 real tests). |
| Frontend unit tests | 79/79 | `npm run test` (Vitest), 0 failures. |
| DAST (OWASP ZAP, local demo stack, unauthenticated) | 119/119 passive+active checks, 0 warnings after fix-and-reverify | `docs/security/DAST.md`. |
| DAST (OWASP ZAP, local demo stack, authenticated) | 119/119, 0 WARN, 0 FAIL | New in the 2026-08-18 follow-up pass — see §7a and `docs/security/DAST.md`. |

## 3. Coverage matrix — full security program

Statuses: **PASS** (implemented and verified this pass), **FAIL** (confirmed defect, listed in
§4), **BLOCKED** (cannot be verified without something that doesn't exist yet — a domain, a real
payment provider, an authorized tester), **NOT APPLICABLE** (no code path exists to test, with
justification).

### 1. Two-user authenticated IDOR / ownership suite

| Control | Status | Evidence |
| --- | --- | --- |
| Questionnaire submissions/answers/guidance/follow-ups — cross-user 404, DB-state unchanged after rejected mutation | **PASS** | `QuestionnaireCrossUserAccessTests` (new, 6 tests) — HTTP-level via the real controller pipeline. Found and fixed a real gap in shared test infra (`QuestionnairesApiTestHost` had no exception-handler middleware, so ownership-check exceptions leaked as raw .NET exceptions instead of 404s — invisible until this pass added a test that actually hit that path). |
| Invoices/purchases cross-user denial | **PASS** | Pre-existing `BillingCrossUserAccessTests` (404/200/401 over real HTTP). |
| Program entitlements scoped to (UserId, ProgramId), not either alone | **PASS** | New `Entitlement_is_scoped_to_both_user_and_program_not_either_alone` against the REAL `BillingProgramAccessContext` (not a fake), two real users, two real programs. |
| Progress records cross-user isolation | **PASS** | Pre-existing `Progress_for_one_user_is_isolated_from_another_users_progress_on_the_same_item`. |
| Event registrations — cancel with no own registration fails closed, leaves the other user's registration untouched | **PASS** | New `Canceling_with_no_registration_of_ones_own_fails_and_leaves_another_users_registration_untouched`. `CancelRegistrationHandler` is structurally scoped by `(eventId, JWT-derived userId)` — there is no client-suppliable "registration id" to attack. |
| Video playback / protected content entitlement (own resource of another user) | **PASS** | Pre-existing `Video_playback_requires_access_to_the_owning_program_not_just_any_program` — proves denial for both "owns a different program" and "owns nothing" cases. |
| Chat — cross-user message/room access | **NOT APPLICABLE** | Rooms are fixed and shared by design (architecture §33); every write uses the JWT `sub` claim, never a client-supplied user id (structural mass-assignment prevention). No owner-scoped resource id exists to attack. |
| User-scoped file/download endpoints | **NOT APPLICABLE** | No upload/download endpoint exists — see `docs/security/UPLOAD_SECURITY_CHECKLIST.md`. |
| Administrators have no implicit access to questionnaire answers/guidance | **PASS** | Pre-existing `QuestionnaireAdminAccessAuthorizationTests`. |
| Random IDs and valid-but-foreign IDs both tested | **PASS** | `QuestionnaireCrossUserAccessTests` tests both a real other-user submission id and `Guid.NewGuid()`. |

### 2. Authentication and token lifecycle

| Control | Status | Evidence |
| --- | --- | --- |
| Refresh-token rotation | **PASS** | Pre-existing `Rotates_the_token_keeping_the_same_family`. |
| Replay of an already-used refresh token | **PASS** | Pre-existing `Reusing_an_already_rotated_token_revokes_the_whole_family`. |
| Family revocation after reuse | **PASS** | Same test. |
| Expired refresh tokens | **PASS** | Pre-existing `Expired_refresh_token_is_rejected`. |
| Revoked sessions / revoke-all | **PASS** | New `RevokeAllSessionsHandlerTests` (2 tests) — a handler existed with zero test coverage before this pass. |
| Concurrent refresh attempts | **PASS — real bug found and fixed** | Found: `RefreshTokenHandler` had no optimistic concurrency, so two concurrent requests reading the same still-active token before either committed could BOTH successfully rotate it — silently branching two active sessions from one token. Fixed: `RevokedAtUtc` is now a concurrency token (`RefreshTokenConfiguration`); the losing writer gets `DbUpdateConcurrencyException`, mapped to the same safe `REFRESH_TOKEN_INVALID` error. Proved with two tests: a deterministic entity-level race (`Two_contexts_racing_to_revoke_the_same_token_row_the_second_write_fails_with_a_concurrency_conflict`) and a real-concurrency `Task.WhenAll` race against two independent SQLite connections (`Concurrent_refresh_of_the_same_token_never_lets_more_than_one_caller_succeed`), both passing, run 5x to confirm non-flaky. |
| Malformed and tampered JWTs | **PASS** | New `JwtTamperingTests`: malformed strings (5 cases), wrong signing key, tampered payload with re-injected signature, "alg:none" bypass — all rejected 401. |
| JWT with modified permissions/issuer/audience/expiry | **PASS** | Same file: wrong issuer, wrong audience, tampered permission claim, expired token — all rejected. |
| Login enumeration resistance | **PASS** | Pre-existing `Wrong_password_throws_invalid_credentials_without_revealing_which_part_is_wrong` / `Unknown_email_throws_the_same_invalid_credentials_error`. |
| Password-reset token replay and expiry | **PASS** | Pre-existing `Confirm_rejects_an_already_used_token` / `Confirm_rejects_an_expired_token`. |
| Email-verification token replay and expiry | **PASS** | Pre-existing `Expired_token_is_rejected` / `Reusing_an_already_used_token_is_rejected`. |
| Account lockout | **PASS** | Pre-existing `Account_locks_after_the_configured_number_of_failed_attempts_and_clears_after_cooldown`. |
| Rate-limit partitioning and safe 429 responses | **PASS** | Pre-existing abuse test + `RateLimitingExtensions`. |

### 3. Billing and webhook security

| Control | Status | Evidence |
| --- | --- | --- |
| Browser-reported payment state cannot grant access | **PASS (by architecture)** | `Checkout` only accepts `Outcome` (a demo-only success/fail selector, ADR-010), never an amount or a "paid" flag; real state transitions happen server-side via `ProcessProviderEventHandler`. |
| Direct checkout/demo endpoint tampering cannot grant another program | **PASS** | `CreateProgramPurchaseCommand` has no amount/currency field — `Checkout_ignores_any_client_supplied_amount_and_always_uses_the_server_side_offer_price`. |
| Entitlement scoped to user ID and program ID | **PASS** | New `Entitlement_is_scoped_to_both_user_and_program_not_either_alone` (see §1). |
| Duplicate callbacks idempotent | **PASS** | Pre-existing `Duplicate_event_delivery_grants_a_single_entitlement`, `Concurrent_duplicate_event_delivery_processes_exactly_once`. |
| Replay/out-of-order events fail safely | **PASS** | Pre-existing `Out_of_order_event_does_not_regress_state`. |
| Refund/chargeback preserves history | **PASS** | Pre-existing `Refund_flips_status_and_revokes_access_without_deleting_history`. |
| Amount/currency not client-tamperable | **PASS** | See above. |
| Real Stripe signature verification, timestamp tolerance, replay | **NOT APPLICABLE today / REQUIRED BEFORE STRIPE PRODUCTION** | No Stripe integration exists (ADR-010). Full test specification written: `docs/security/STRIPE_WEBHOOK_TEST_SPEC.md` — 10 required test rows, none may be marked PASS until real Stripe test-mode webhooks exist. |

### 4. Upload and file-security coverage

| Control | Status | Evidence |
| --- | --- | --- |
| All 14 checklist items (MIME/extension validation, size limits, path traversal, storage-key isolation, signed URL expiry, etc.) | **NOT APPLICABLE** | Repo-wide search confirms zero upload endpoints exist — `Files` module is an empty scaffold. Full mandatory pre-launch checklist written: `docs/security/UPLOAD_SECURITY_CHECKLIST.md`, becomes required acceptance criteria the moment a first upload endpoint is proposed. |

### 5. Sensitive-data leakage and log inspection

| Control | Status | Evidence |
| --- | --- | --- |
| Passwords/tokens/authorization headers never in logs | **PASS — live-verified against real running logs** | `scripts/security/Test-LogLeakage.ps1`, executed against the live Docker container with unique synthetic canaries in a password, a Bearer token, and a refresh token — `docker logs bunited-api` searched for all three, zero found. |
| Real DTOs actually redact if ever logged | **PASS — real gap found and fixed** | Found: `SensitiveLogValueAttribute`/`SensitiveDataDestructuringPolicy` existed but were applied to ZERO real production types — only a synthetic test class. Fixed: applied to `LoginCommand.Password`, `RegisterUserCommand.Password`, `RefreshTokenCommand.RefreshToken`, `ConfirmPasswordResetCommand.Token`/`NewPassword`. New `SensitiveCommandLoggingTests` (4 tests) prove the real Serilog policy actually redacts these real types. |
| Questionnaire answers / guidance text excluded from logs | **PASS (structural, verified by code read)** | No handler in the questionnaire flow ever passes answer/guidance content to a logger call — confirmed by reading every Client-facing handler. |
| Structured errors expose stable codes/messageKey/correlationId, no stack traces | **PASS** | Pre-existing `GlobalExceptionHandlerTests`; reconfirmed live via ZAP's "Application Error Disclosure" and "Information Disclosure - Debug Error Messages" passive rules (both PASS). |
| Audit logs metadata-only for sensitive questionnaire operations | **PASS (structural, per ADR-006/architecture §17-18)** | `AuditEntry` never carries answer/guidance text; verified by code read of every `AuditLogger` call site in the Questionnaires module. |
| Security tests never write secrets into Playwright artifacts | **PASS** | Reviewed `frontend/e2e/support/audit.ts` and every security spec — passwords/tokens are used inline in requests, never `console.log`'d, never attached as test artifacts. |

### 6. Dependency and static-analysis security gates

| Control | Status | Evidence |
| --- | --- | --- |
| `dotnet list package --vulnerable --include-transitive` | **PASS — real CVE found and fixed** | Found: `Newtonsoft.Json 11.0.1` (transitive via `Hangfire.Core`) vulnerable to CVE-2024-21907 (GHSA-5crp-9r3c-p9vr, High/CVSS 7.5, DoS via deep JSON nesting), present in the production `Api`/`Migrations`/`Events.*` projects. Fixed: explicit `Newtonsoft.Json 13.0.4` pin in `Events.Infrastructure.csproj`; re-scan confirms zero vulnerable packages in every non-test project solution-wide. Remaining: `SQLitePCLRaw.lib.e_sqlite3 2.1.10` (CVE-2025-6965, High) — **test-only** (in-memory SQLite for xUnit, never shipped in the production container which uses Npgsql), no patched NuGet version exists upstream yet; tracked, not fixed (nothing to fix). |
| `dotnet list package --deprecated` | **PASS (informational, no action needed)** | Only `xunit 2.9.2` flagged "Legacy" in favor of `xunit.v3` — not a security deprecation, a test-only dev dependency, out of scope for a broad migration this pass. |
| `npm audit` with explicit release threshold | **PASS** | `npm audit`: 0 vulnerabilities (234 total dependencies). CI now runs `npm audit --audit-level=high` as a release gate (`.github/workflows/ci.yml`). |
| Dependency review on PRs | **UNVERIFIED — workflow added, never executed** | `dependency-review` job added to `ci.yml` using `actions/dependency-review-action@v4`, `fail-on-severity: high`. YAML-validated (`js-yaml` parse) only; no PR has ever been opened against this repo on GitHub, so this workflow has never actually run on a GitHub Actions runner. Do not treat as PASS until a real executed run exists. |
| Lint/static checks for unsafe HTML, token persistence | **PASS** | `dangerouslySetInnerHTML` usage audited: the one real usage (`ProgramPlayerPage.tsx`) is DOMPurify-sanitized. Token storage audited: only the rotating, revocable refresh token is persisted (`localStorage`); the access token is memory-only — a pre-existing, documented design (see §11 residual risk). |
| Secret scanning | **UNVERIFIED — workflow added, never executed** | `secret-scan` job added to `ci.yml` using `gitleaks/gitleaks-action@v2`. YAML-validated only, never run by a real GitHub Actions runner — there is no executed-run history to point to in this environment (no `gh`/Actions access). Not GitHub's native secret-scanning/push-protection either (a repository setting, not a workflow — enable it in repo Settings → Security once the repo is pushed to GitHub with that feature available). |
| SAST/CodeQL | **UNVERIFIED — workflow added, never executed** | New `.github/workflows/codeql.yml` — C# and JavaScript/TypeScript matrix, push/PR/weekly schedule. YAML-validated only, never run by a real Actions runner — no executed-run evidence exists. |
| Generated reports not committed with sensitive/machine-specific info | **PASS** | No scan report file was committed — all results are hand-written into this document and `docs/security/DAST.md`, not raw tool output. |

### 7. CSP and browser security policy

| Control | Status | Evidence |
| --- | --- | --- |
| Full source inventory (script/style/img/font/connect/frame) | **PASS** | `frontend/vite.config.ts`'s `cspMetaTagPlugin` — documented inline, mirrored in §7 of this matrix's evidence trail. |
| Distinguishes production API from Development-only Swagger | **PASS** | CSP is a frontend concern (SPA `<meta>` tag); the Api's own Swagger exposure is separately gated to `Development` (pre-existing) and verified absent-in-non-dev by `Test-ProductionSecurity.ps1`. |
| No `unsafe-eval` in production | **PASS — real dependency found and fixed** | Found: Zod v4's own internal `Function("")` eval-capability probe triggered a real `securitypolicyviolation` in the PRODUCTION build. Fixed: `config({ jitless: true })` (Zod's own documented escape hatch for exactly this) in `src/shared/zodJitlessConfig.ts`, imported first in `main.tsx`. Verified: production build (`npm run build` + `vite preview`) loads the full client journey with **zero** CSP violations (`csp.spec.ts`). |
| No `unsafe-inline` in production (dev-only exception, justified) | **PASS** | Same verification. `npm run dev` (never shipped to users) needs `'unsafe-inline'`/`'unsafe-eval'` for Vite's React Fast Refresh preamble and HMR — isolated to `serve` mode only via `vite.config.ts`'s `context.server !== undefined` check, confirmed absent from the built `dist/index.html`. |
| `default-src`, `script-src`, `style-src`, `img-src`, `font-src`, `connect-src`, `frame-src`, `object-src`, `base-uri`, `form-action` all defined | **PASS** | `csp.spec.ts`'s directive-presence test. |
| `frame-ancestors` | **BLOCKED — REQUIRES DEPLOYED DOMAIN** | The CSP spec requires browsers to ignore `frame-ancestors` delivered via `<meta>` — it only works as a real HTTP header, which requires knowing the real static-host/reverse-proxy config. The Api's own `X-Frame-Options: DENY` already protects the Api's own responses. |
| `upgrade-insecure-requests` | **BLOCKED — REQUIRES DEPLOYED DOMAIN** | Would break local HTTP dev/demo; only correct once the SPA is actually served over HTTPS. |
| Automated header tests | **PASS** | `csp.spec.ts` (2 tests) + `SecurityHeadersMiddlewareTests` (backend headers). |
| Playwright proves the SPA loads and critical flows work under CSP | **PASS** | `csp.spec.ts`'s first test: full login + all 7 primary routes, zero violations, against the real production build. |
| Capture browser CSP violations, fail on unexpected ones | **PASS** | Same test — captures every `securitypolicyviolation` event and fails on any. |

### 8. Production TLS, HSTS, CORS, proxy, and domain verification

| Control | Status | Evidence |
| --- | --- | --- |
| Forwarded headers / trusted proxy configuration | **PASS — real dangerous default found and fixed** | Found: ASP.NET Core's `ForwardedHeadersMiddleware` treats an EMPTY `KnownProxies`/`KnownNetworks` as "trust every caller's forwarded headers unconditionally" — the opposite of safe-by-default (proved by a dedicated test). Fixed: `ForwardedHeadersExtensions` now skips registering the middleware entirely when nothing is configured (a true no-op) rather than relying on that dangerous empty-list behavior; configurable via `ForwardedHeaders:KnownProxies`/`KnownNetworks`. 3 new tests. |
| HTTPS redirection behind a reverse proxy | **BLOCKED — REQUIRES DEPLOYED DOMAIN** | `UseHttpsRedirection()` is registered; real-world correctness behind a proxy depends on the forwarded-headers config above being set to the real proxy IP, which requires knowing the real topology. |
| HSTS only outside Development | **PASS** | Pre-existing `if (!app.Environment.IsDevelopment()) { app.UseHsts(); }`, unchanged this pass. |
| HSTS max-age and IncludeSubDomains | **PASS (configured, unverified against real domain)** | `AddHsts(options => { MaxAge = 365 days; IncludeSubDomains = true; })`. `Test-ProductionSecurity.ps1` checks `max-age >= 180 days` once a real domain exists. |
| Explicit production CORS origins | **PASS** | `CorsExtensions` always builds from an explicit `Cors:AllowedOrigins` allow-list, never `AllowAnyOrigin`. |
| Rejection of wildcard origins | **PASS — real gap found and fixed** | Found: ASP.NET Core's `CorsPolicyBuilder.WithOrigins("*")` genuinely enables wildcard matching — not an inert literal string, contrary to the natural assumption (proved by a test). Fixed: `CorsExtensions.ConfigurePolicy` now explicitly filters out a literal `"*"` before calling `WithOrigins`. 3 new tests confirm wildcard-in-config, empty-config, and exact-origin-only behavior. |
| Rejection of hostile origins | **PASS** | `CorsExtensionsTests` + live-confirmed via `security-extended.spec.ts` and the ZAP scan. |
| No credentialed wildcard CORS | **PASS** | `AllowCredentials()` is never called anywhere in the codebase (repo-wide search) — Bearer-token auth, not cookies. |
| Correct scheme detection behind the proxy | **BLOCKED — REQUIRES DEPLOYED DOMAIN** | Depends on the forwarded-headers config above being pointed at the real proxy. |
| Secure cookie behavior | **NOT APPLICABLE** | No cookies are used anywhere — auth is Bearer-token-in-header (verified: zero `Set-Cookie`/`CookieOptions` usage in the codebase). |
| Production Swagger/OpenAPI exposure | **PASS (code-gated), BLOCKED (live verification needs a domain)** | Code already restricts to `Development`; `Test-ProductionSecurity.ps1` checks it live once a domain exists. |
| Production demo accounts and fake providers disabled | **PASS (code-gated), BLOCKED (live verification needs a domain)** | `VerifyNoDemoOnlyAdaptersInProduction` (pre-existing P3.32) + `DemoAccountSeeder`'s own Production gate; `Test-ProductionSecurity.ps1` checks demo-credential rejection live once a domain exists. |
| Placeholder secrets rejected at startup | **PASS — real gap found and fixed** | Found: the JWT signing-key length check alone did not catch `.env.example`'s own documented placeholder value (58 chars — long enough to pass). Since `.env.example` is committed to the repo, deploying with that literal value would let anyone forge a valid JWT. Fixed: `AddIdentityJwtAuthentication` now explicitly rejects that exact string in `Production`. 4 new tests. |
| Production verification runbook/script | **PASS (script written and smoke-tested), BLOCKED (never run against a real domain)** | `scripts/security/Test-ProductionSecurity.ps1` + `docs/security/PRODUCTION_VERIFICATION_RUNBOOK.md`. Smoke-tested end-to-end against `localhost:8080` — every environment-conditional check reported the environment-appropriate result (see the runbook's own results table), and every check that must hold everywhere (defensive headers, CORS specificity) PASSED. |

### 9. DAST and adversarial API checks

| Control | Status | Evidence |
| --- | --- | --- |
| Safe local DAST workflow, local/demo target only | **PASS — actually executed, not just documented** | OWASP ZAP against the local Docker Compose demo stack. See `docs/security/DAST.md` for full methodology, results, and the fix-and-reverify cycle (2 real findings, both fixed and confirmed gone on re-scan). |
| Non-destructive payloads, redacted auth material | **PASS** | ZAP's baseline/active-scan payloads are non-destructive proof-of-concept probes by design (same category as the existing Playwright injection probes); no authentication material was used in this scan (unauthenticated pass — see coverage gaps in `DAST.md`). |
| Machine-readable report saved | **PARTIAL** | Console summary captured in full in `DAST.md`; file-based HTML/JSON export was attempted but the bind-mount path did not resolve in this session's sandboxed shell — a session-environment limitation, not a ZAP limitation (documented with exact reproduction commands for a normal host shell). |
| Classify findings by severity, verify false positives manually | **PASS** | Both findings manually assessed (informational/low), both fixed anyway since they were cheap to close. |
| Fail on confirmed high/critical, don't fail on informational noise | **PASS** | Zero high/critical findings from either scan; the two low/informational findings were fixed rather than either failing the release or being silently ignored. |
| Existing Playwright injection probes retained, unmodified | **PASS** | `security-extended.spec.ts` unchanged — SQL/time-based injection, reflected XSS, CRLF/header injection, path traversal, malformed identifiers, method override, unsupported media types, hostile-origin CORS all still present and passing. |

### 10. External penetration test

| Control | Status | Evidence |
| --- | --- | --- |
| Scope document (environments, in-scope modules, exclusions, accounts, data handling, reporting, severity, retest, stop conditions) | **PASS (document written)** | `docs/security/PENTEST_SCOPE.md`. |
| The actual penetration test | **BLOCKED — REQUIRES AUTHORIZED EXTERNAL TESTER AND DEPLOYED ENVIRONMENT** | No domain exists, no tester has been engaged. Never converted to PASS based on automated tests — see the scope document's own closing statement. |

### 11. Reporting model

| Control | Status | Evidence |
| --- | --- | --- |
| Two separate dimensions (automated score vs. full program coverage) | **PASS** | This document's structure — §2 (automated scores) vs. §3 (full coverage matrix) are kept explicitly distinct. |
| PASS/FAIL/BLOCKED/NOT APPLICABLE statuses used throughout | **PASS** | See §3. |
| 100/100 qualified with the mandatory statement | **PASS** | See the top of this document. |

## 4. Confirmed findings, ordered by severity

All of the following were found DURING this pass (not carried over from the prior audit) and are
**fixed and reverified**, not just reported:

1. **High — ASP.NET Core CORS `WithOrigins("*")` genuinely enables wildcard matching.** A
   misconfiguration risk: if `Cors:AllowedOrigins` ever contained a literal `"*"`, the app would
   silently allow any origin. Fixed in `CorsExtensions`; 3 regression tests.
2. **High — `ForwardedHeadersMiddleware` trusts every caller when `KnownProxies`/`KnownNetworks`
   are empty.** The opposite of safe-by-default; would have let a hostile client spoof its IP/
   scheme behind an unconfigured or misconfigured reverse-proxy setting. Fixed by not registering
   the middleware at all when unconfigured; 3 regression tests.
3. **High — `Newtonsoft.Json 11.0.1` (CVE-2024-21907, DoS) in the production Api/Migrations/
   Events projects**, transitive via Hangfire. Fixed with an explicit version pin; re-scan
   confirms zero vulnerable packages solution-wide outside test-only projects.
4. **Medium — concurrent `/auth/refresh` requests on the same token could both succeed**, silently
   branching two active sessions from one token (no optimistic concurrency). Fixed with a
   concurrency token on `RevokedAtUtc`; 2 regression tests including a real-concurrency race.
5. **Medium — the JWT signing-key placeholder from `.env.example` would pass the length check.**
   Since that file is committed to the repo, deploying with the placeholder still in place would
   let anyone forge valid JWTs. Fixed with an explicit Production-only rejection; 4 regression
   tests.
6. **Medium — `SensitiveLogValueAttribute` redaction existed but was applied to zero real
   production types.** No defense-in-depth against an accidental future `logger.LogInformation("{@Command}",
   command)` on a password/token-bearing command. Fixed by applying it to the four real commands
   that carry passwords/tokens; 4 regression tests.
7. **Medium — Zod's internal eval-capability probe violated the production CSP's `script-src`
   (no `unsafe-eval`).** Fixed with Zod's own documented `jitless` config escape hatch; verified
   zero violations against the real production build.
8. **Low — `QuestionnairesApiTestHost` (shared test infrastructure) had no exception-handler
   middleware**, so ownership-check `NotFoundAppException`s leaked as raw .NET exceptions in
   tests instead of the real API's 404 — meaning this shared host could not have caught a real
   controller wiring bug for any 404/400/401/403 path. Fixed; all admin-authorization tests using
   the same host continue to pass unchanged.
9. **Low — 404 responses lacked explicit `Cache-Control: no-store`; `Cross-Origin-Resource-Policy`
   was absent on every response.** Found by a real OWASP ZAP scan. Fixed in
   `SecurityHeadersMiddleware`; re-scan confirms both warnings gone (119/119 PASS, 0 WARN).

No FAIL-severity finding remains unresolved. No finding was silently downgraded or removed from
scope to make a number look better — the two DAST warnings were fixed rather than argued away, and
every "PASS" in §3 that required a code change says so explicitly.

## 5. Exact files changed

Production code:
- `src/BuildingBlocks/Security/Headers/SecurityHeadersMiddleware.cs` (Cross-Origin-Resource-Policy, Cache-Control)
- `src/BuildingBlocks/Security/Proxy/ForwardedHeadersExtensions.cs` (new)
- `src/BuildingBlocks/Security/Cors/CorsExtensions.cs` (wildcard rejection)
- `src/Api/Program.cs` (wires `UseBUnitedForwardedHeaders`, passes `builder.Environment` to `AddIdentityModule`)
- `src/Modules/Identity/Infrastructure/IdentityModuleExtensions.cs` (`IHostEnvironment` param)
- `src/Modules/Identity/Infrastructure/Security/JwtAuthenticationExtensions.cs` (placeholder-key rejection)
- `src/Modules/Identity/Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` (concurrency token)
- `src/Modules/Identity/Application/UseCases/Refresh/RefreshTokenHandler.cs` (concurrency-exception handling)
- `src/Modules/Identity/Application/UseCases/Login/LoginCommand.cs`, `.../Register/RegisterUserCommand.cs`, `.../Refresh/RefreshTokenCommand.cs`, `.../PasswordReset/ConfirmPasswordResetCommand.cs` (`[SensitiveLogValue]`)
- `src/Modules/Events/Infrastructure/BUnited.Modules.Events.Infrastructure.csproj` (Newtonsoft.Json pin)
- `frontend/index.html` (CSP `<meta>` now injected by a plugin, not static)
- `frontend/vite.config.ts` (`cspMetaTagPlugin`, mode-aware CSP)
- `frontend/src/main.tsx`, `frontend/src/shared/zodJitlessConfig.ts` (new — Zod jitless config)

Test/tooling code:
- `src/BuildingBlocks/Security.Tests/` (new project: `ForwardedHeadersExtensionsTests`, `CorsExtensionsTests`, plus prior-session `SecurityHeadersMiddlewareTests` updated)
- `src/Modules/Identity/Tests/UseCases/RefreshTokenHandlerTests.cs` (2 new concurrency tests), `RevokeAllSessionsHandlerTests.cs` (new)
- `src/Modules/Identity/Tests/Security/JwtTamperingTests.cs`, `ProductionSecretSafetyTests.cs`, `SensitiveCommandLoggingTests.cs` (new)
- `src/Modules/Questionnaires/Tests/Security/QuestionnaireCrossUserAccessTests.cs` (new), `TestSupport/QuestionnairesApiTestHost.cs` (exception handler + program-access/consent wiring)
- `src/Modules/Events/Tests/Application/EventRegistrationFlowTests.cs` (1 new test)
- `src/Modules/Billing/Tests/Application/ProgramCommerceFlowTests.cs` (1 new test)
- `frontend/e2e/csp.spec.ts` (new)
- `scripts/security/Test-LogLeakage.ps1`, `Test-ProductionSecurity.ps1` (new)
- `BUnited.sln` (new test project registered)

Documentation/CI:
- `docs/security/STRIPE_WEBHOOK_TEST_SPEC.md`, `UPLOAD_SECURITY_CHECKLIST.md`, `DAST.md`, `PENTEST_SCOPE.md`, `PRODUCTION_VERIFICATION_RUNBOOK.md` (all new)
- `docs/E2E_AUDIT_RESULT.md` (this file), `docs/TASKS.md`
- `.github/workflows/ci.yml` (dependency scan, `dependency-review`, `secret-scan` jobs), `.github/workflows/codeql.yml` (new)

## 6. Exact commands executed and results

| Command | Result |
| --- | --- |
| `dotnet build BUnited.sln` | Build succeeded, 0 warnings, 0 errors (run repeatedly through the session; final run clean). |
| `dotnet test BUnited.sln --configuration Release` | **447/447 passed**, 0 failed, across 14 test projects with tests (2026-08-18 follow-up pass: `Notifications.Tests` went from 0 to 15 real tests — see §7a. `Files.Tests` remains 0 tests: the Files module is still a documented, intentional empty scaffold with no implementation to test at all, confirmed by re-reading every layer's source tree — see §7a for detail; padding it with fake tests was rejected as it would violate DEVELOPMENT_INSTRUCTIONS.md §9's "no trivial/artificial tests" rule.) |
| `dotnet list BUnited.sln package --vulnerable --include-transitive` (before/after fix) | Before: `Newtonsoft.Json 11.0.1` (High) in `Api`/`Migrations`/`Events.Infrastructure`/`Events.Api`; `SQLitePCLRaw.lib.e_sqlite3 2.1.10` (High) in every `*.Tests` project. After: zero vulnerable packages outside `*.Tests` projects; SQLitePCLRaw unchanged (no upstream fix available, test-only). |
| `dotnet list BUnited.sln package --deprecated` | Only `xunit 2.9.2` → `xunit.v3` (informational, test-only, no action taken). |
| `npm run build` (frontend, `VITE_API_BASE_URL=http://localhost:8080/api/v1`) | Succeeded; `dist/index.html`'s CSP confirmed to contain no `unsafe-eval`/`unsafe-inline`. |
| `npm run test` (Vitest) | **79/79 passed**, 24/24 files. |
| `npm run check:locale-parity` | `✓ Locale key parity OK (10 namespace files, ro/en).` |
| `npm audit` | 0 vulnerabilities (234 dependencies). |
| `git diff --check` | Exit 0 — no whitespace/conflict-marker errors (only pre-existing CRLF-on-checkout warnings). |
| `scripts/security/Test-LogLeakage.ps1` (against the live Docker container) | `PASS: no password, bearer token, or refresh token canary found in bunited-api logs.` |
| `scripts/security/Test-ProductionSecurity.ps1 -ApiUrl http://localhost:8080 -SpaUrl http://localhost:5173` | 6 environment-conditional FAILs, all environment-correct (see `PRODUCTION_VERIFICATION_RUNBOOK.md`'s results table); 8 universal checks PASS. |
| OWASP ZAP `zap-baseline.py` + `zap-api-scan.py` (before/after fix) | Before: 66+118 PASS, 2 WARN total. After rebuild: 119/119 PASS, 0 WARN. Full detail in `docs/security/DAST.md`. |
| `npx playwright test` (canonical run — all 3 projects, single invocation, `E2E_API_BASE_URL=http://localhost:8080/api/v1`) | **17/17 passed, exit code 0, ~33s, no hang** — reproduced on 3 consecutive real runs during the 2026-08-18 follow-up pass (including an immediate back-to-back rerun exercising the new `/auth/login` 429-backoff-retry in `global-setup.ts`). See §7a for the root-cause fix (previously 16/17, `mobile-chromium`'s login starved by the auth rate limiter — see below). |

Any single-file/single-project invocation (e.g. `npx playwright test e2e/ui-ux.spec.ts --project=desktop-chromium`) is a **focused run — diagnostic only, not canonical**; `frontend/e2e-results/score.md` now labels every run's type explicitly (canonical vs focused) so a focused diagnostic result can never be mistaken for, or blended into, the canonical score (see §7a).

## 7. Dependency/SAST/DAST findings summary

- **Dependency scan**: 1 real High-severity CVE found and fixed (Newtonsoft.Json/CVE-2024-21907);
  1 High-severity CVE found with no available fix, confined to test-only code
  (SQLitePCLRaw/CVE-2025-6965); 0 frontend vulnerabilities.
- **SAST (CodeQL)**: workflow added, never executed by a real Actions runner this session —
  BLOCKED on a GitHub Actions run, not on unwritten configuration.
- **Secret scanning (Gitleaks)**: workflow added, never executed by a real Actions runner this
  session. A manual review of every new/changed file in this session found no committed secret.
- **DAST (OWASP ZAP)**: 2 real low/informational findings, both found, fixed, and reverified with
  a clean re-scan. Zero SQL injection, XSS, XXE, SSTI, command injection, path traversal, CRLF
  injection, or known-CVE-signature finding across 185 combined active/passive rule checks.

## 7a. 2026-08-18 follow-up pass: fixes from an independent audit

A separate independent audit re-verified this document's claims and found several real gaps.
This section records what was actually fixed, with real re-verified numbers — it does not
re-litigate §1-§7 above, which stand except where corrected here.

- **Playwright canonical run fixed (was 16/17, now 17/17).** Root cause: `/auth/login` carries a
  5-request/minute/IP rate limit (`RateLimitingExtensions.AuthPolicyName`); the four specs that
  needed an authenticated client/expert session (`flow.spec.ts` x2, `csp.spec.ts`,
  `ui-ux.spec.ts`) each drove the real login form, and Playwright's own file-scheduling order
  across projects didn't reliably keep `abuse.spec.ts`'s deliberate 429 test last, so a later
  project's login could get starved. Fix: `frontend/e2e/global-setup.ts` now authenticates each
  browser-context consumer once via a direct API call (5 real `/auth/login` calls total — one per
  distinct browser context that needs one; refresh tokens are single-use/rotating and cannot be
  "forked", so each context genuinely needs its own login) and persists a Playwright
  `storageState` file per consumer; specs load that state instead of driving the login form, and
  the app's own silent-refresh-on-load bootstraps the session via `/auth/refresh`, which carries
  no rate limit. `playwright.config.ts` also declares explicit project `dependencies` so
  `security-abuse-last` is guaranteed to run strictly after `desktop-chromium`/`mobile-chromium`
  regardless of scheduling order, and `global-setup.ts` backs off and retries (using the server's
  own `Retry-After` header) if `/auth/login` is ever still rate-limited — verified live: an
  immediate back-to-back rerun within the same 60s window would otherwise 429 during setup itself;
  the retry made that rerun pass too. The production rate limiter itself was never touched or
  weakened. Separately fixed: `playwright.config.ts` used `__dirname`, which doesn't exist under
  this project's `"type": "module"` ESM mode (crashed config loading) — replaced with
  `fileURLToPath(import.meta.url)`; and the `webServer` command changed from `npm run dev` to
  invoking `node node_modules/vite/bin/vite.js` directly, because `npm run` on Windows spawns
  cmd.exe → npm → node and Playwright terminating the top process didn't reliably kill the
  grandchild Vite process, leaving `npx playwright test` hanging after the run — confirmed fixed:
  3 consecutive real runs, including a script-managed webServer start, all exited cleanly (code 0).
- **`Files.Tests`/`Notifications.Tests` previously had zero test files (real gap).**
  `Notifications.Tests` now has 15 real tests (`LoggingNotificationSenderTests`,
  `NotificationsModuleExtensionsTests`) covering: `templateData` (which per its own contract may
  carry submission ids resolving to guidance/questionnaire content) is never logged, in either the
  rendered message or the structured log state; every `NotificationType` completes without
  throwing; the sender is gated from `Production` via `IDemoOnlyAdapter` (locks in the P3.32
  startup-safety contract); and the DI registration resolves the right implementation at the right
  lifetime. `Files.Tests` remains at 0 tests — re-confirmed by re-reading the module's full source
  tree (`src/Modules/Files/**/*.cs` contains no source files at all, only build output, across
  every layer: Domain/Application/Infrastructure/Api/Contracts), consistent with `docs/TASKS.md`'s
  own prior findings (P7.04.b, P7.22.c). There is no real behavior to test; adding fake tests would
  violate DEVELOPMENT_INSTRUCTIONS.md §9's "no trivial/artificial tests" rule. This is a documented
  decision, not a silently-skipped gap.
- **2 lint warnings fixed, `npm run lint` now produces zero output.**
  `frontend/e2e/ui-ux.spec.ts`'s `async ({}, testInfo) =>` triggered oxlint's empty-pattern rule;
  Playwright's own test-callback contract requires the first argument to literally be an object
  destructuring pattern (it statically inspects the signature), so the empty pattern is correct
  and kept, with a targeted `eslint-disable-next-line no-empty-pattern` and a one-line
  justification. `frontend/src/modules/content/YouTubePlayer.tsx`'s cleanup closure read
  `wrapperRef.current` directly (react-hooks/exhaustive-deps: a ref read inside a cleanup
  function) — fixed by capturing the ref's value into a local `wrapperElement` at effect-setup
  time and using that captured value throughout the effect and its cleanup, the standard fix for
  this exact warning.
- **`scratch_token.txt` removed; `.gitignore` updated.** It was untracked (confirmed via
  `git status` before deleting — a plain file delete, no git operation). Added an explicit
  `scratch_token.txt` entry (not a broad glob) plus `frontend/e2e/.auth/` (the new per-run
  storageState directory, which holds real session refresh tokens and must never be committed).
- **Main frontend bundle reduced from 642.50 kB (gzip 179.73 kB) to 229.94 kB (gzip 70.98 kB).**
  `frontend/src/app/router.tsx` had zero route-level code-splitting — every screen (client and
  admin) was statically imported into one entry chunk. Converted every route screen to
  `React.lazy(() => import(...))`, wrapped `<Routes>` in a `<Suspense>` with a loading fallback
  that matches `SessionProvider`'s own bootstrap loading state (no blank-flash regression). `npm
  run build` no longer emits the "chunks larger than 500 kB" warning; `npm run test -- --run`
  still passes 79/79 with no regression.
- **CI's NuGet vulnerability scan is now a real release gate for production code.**
  `.github/workflows/ci.yml`'s dependency-scan step previously used `continue-on-error: true`
  unconditionally (informational only, per the original comment). It now parses
  `dotnet list package --vulnerable --include-transitive --format json` and fails the job only if
  a High/Critical finding traces to a project whose path does NOT end in `.Tests.csproj`;
  test-only findings (the known `SQLitePCLRaw.lib.e_sqlite3`/GHSA-2m69-gcr7-jv3q, High, present
  only in `*.Tests.csproj` projects — re-confirmed via a fresh scan) are printed as informational
  and do not block. Verified locally against the real current vulnerability JSON: the gate logic
  correctly classifies all 10 current findings as test-only/informational and would exit 1 if any
  were in a non-test project.
- **CodeQL/Gitleaks/dependency-review workflows corrected from "PASS" to "UNVERIFIED"** in the
  table in §6/§7 below — YAML-validated only, never executed by a real GitHub Actions runner in
  this or any prior session (no `gh`/Actions access in this environment). This was a real
  overclaim in the prior version of this document; it is not implied to be equivalent to a passing
  CI run anywhere else in this document either.
- **New authenticated ZAP DAST pass added**, extending the previously unauthenticated-only scan.
  `scripts/security/zap-authenticated-scan.ps1` logs in as the seeded local demo client
  (`src/Migrations/Seed/DemoAccountSeeder.cs` — no new/invented credentials) via a real
  `/auth/login` call, then runs `zap-api-scan.py` against the Api's OpenAPI document with ZAP's
  Replacer add-on injecting `Authorization: Bearer <token>` on every request the scanner makes.
  Deliberately does not write `-r`/`-J` report files (they would echo the Authorization header
  into the saved report) — only the console PASS/WARN/FAIL summary is kept, and the token itself
  is never printed or written to disk. Real result from this pass: **119 PASS, 0 WARN, 0 FAIL**
  (same rule set as the unauthenticated `zap-api-scan.py` run in `docs/security/DAST.md` — most
  business endpoints still require additional context beyond a bearer token, e.g. path-specific
  resource ids, that a generic authenticated crawl doesn't supply, so this extends confidence but
  does not replace the code-level IDOR/ownership test suites). Full detail and reproduction
  command in `docs/security/DAST.md`.

## 8. Production-environment blockers

Every item below is blocked strictly because no B-United production/staging domain exists — not
because of missing code, missing tests, or missing documentation:

- Real TLS certificate, protocol version, and HTTPS-redirect verification (`Test-ProductionSecurity.ps1` is ready; needs a domain).
- Real HSTS header verification against a live HTTPS listener.
- `frame-ancestors`/`upgrade-insecure-requests` CSP directives (require knowing the real static-host/reverse-proxy layer).
- Forwarded-headers `KnownProxies`/`KnownNetworks` configuration (requires knowing the real reverse-proxy IP/network).
- Live confirmation that Swagger/OpenAPI/demo-credentials are actually disabled in the real Production deployment (code-gated already; needs live confirmation).
- Real Stripe webhook signature/timestamp/replay verification (Phase 8, requires the real provider integration to exist first — see `docs/security/STRIPE_WEBHOOK_TEST_SPEC.md`).

## 9. External penetration-test status

**BLOCKED — REQUIRES AUTHORIZED EXTERNAL TESTER AND DEPLOYED ENVIRONMENT.** Scope document
written (`docs/security/PENTEST_SCOPE.md`). No engagement has occurred, no domain is authorized
for testing, and no automated result in this document is claimed as equivalent to one.

## 10. Remaining risks

- **Refresh token stored in `localStorage`.** A documented, pre-existing, deliberate trade-off
  (access token is memory-only; only the rotating, revocable refresh token persists) — not
  changed this pass. An XSS-to-token-theft chain remains theoretically possible if a stored-XSS
  vulnerability were ever introduced elsewhere in the app; the existing DOMPurify sanitization on
  the one `dangerouslySetInnerHTML` usage and the strict CSP both reduce that likelihood, but
  don't eliminate the underlying storage trade-off. Flagged for the external pentest's scope.
- **CodeQL and Gitleaks workflows are unexecuted (UNVERIFIED, not PASS).** They will only produce
  real findings, and only become verifiable, once this branch/repo actually runs through GitHub
  Actions — `gh`/Actions access is not available in this environment, so this remains genuinely
  unverifiable here, not merely undone.
- **DAST coverage was unauthenticated for the original pass; a follow-up authenticated pass was
  added and run (§7a), but full authenticated business-logic coverage is still not proven this
  way.** A generic bearer-token-injecting crawl still can't supply the path-specific resource ids
  and ownership context most business endpoints need; the code-level IDOR/ownership test suites
  remain the real proof for authenticated business-logic paths, not either DAST pass.
- **The `zap-baseline.py`/`zap-api-scan.py`/authenticated-scan HTML/JSON report files were not
  saved** due to a session-environment bind-mount limitation (and, for the authenticated scan,
  deliberately — see §7a) — the console summaries are complete and reproduced in full in
  `docs/security/DAST.md`, with exact commands to regenerate file reports on a normal host.
- **The Playwright canonical run's exact-5-login rate-limit budget is tight by design, not
  accidental slack.** `global-setup.ts`'s backoff-retry (§7a) makes an immediate back-to-back
  rerun within the same 60s window succeed (verified live) rather than fail, but it does add a
  real wait in that specific scenario. A single canonical run, the graded scenario, is unaffected.

## 11. What was not verified

- Real production TLS/HSTS/CORS/Swagger/demo-credential behavior (no domain exists).
- Real Stripe webhook security (no Stripe integration exists).
- Anything in the upload-security checklist (no upload endpoint exists).
- CodeQL/Gitleaks/dependency-review findings — **UNVERIFIED**, not PASS: workflows exist and are
  YAML-valid but have never been executed by a real GitHub Actions runner (no `gh`/Actions access
  in this environment, in this or any prior session).
- An external, human-conducted penetration test (not engaged).
- Full authenticated business-logic-path DAST coverage (an authenticated ZAP pass was added and
  run in §7a with a real result, but it does not replace the code-level IDOR/ownership suites).

Do not treat any of the above as PASS. They are explicitly BLOCKED or NOT APPLICABLE in §3, and
this report does not claim otherwise anywhere.
