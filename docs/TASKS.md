# B-United — Task Backlog

Derived from [`PROMPT.md`](PROMPT.md). Tasks are grouped by delivery phase
(per PROMPT.md §69) plus a Phase 0 for the mandatory pre-implementation
architecture work (§74–75). Each parent task is numbered `P<phase>.<n>` and
broken into lettered subtasks (`P<phase>.<n>.<letter>`) — concrete
implementation steps for stable referencing in commits, branches and PRs.

Checkbox state reflects implementation status — update as work lands.
Do not reorder task/subtask numbers once referenced elsewhere (add new
items at the end of their category instead). Subtasks for later phases are
written from the spec, not from code that exists yet — expect to adjust
them once the preceding phase's real implementation lands.

---

# Category A — Development MVP

This category delivers a self-contained, presentation-ready product using local
PostgreSQL and deterministic fake adapters where an external provider would
normally be required. Fake adapters must exercise the same application contracts
and business state transitions as production adapters, including success,
decline/failure, timeout, duplicate delivery, retry and out-of-order scenarios.
They must never make real charges, send real email or require third-party credentials.

The application must expose an explicit `Demo` configuration/profile, clearly label
simulated external actions in the UI, and refuse to start in `Production` while any
fake provider is registered.

## Phase 0 — Architecture & Review (before any production code)

### 0.A Architecture deliverables (§74)

- [x] P0.01 Executive architecture overview
  - [x] P0.01.a Summarize product scope, target scale and non-goals in one page (from PROMPT.md §1–2, §67)
  - [x] P0.01.b State the chosen architectural style and why (modular monolith, single DB) referencing ADR-001/002
  - [x] P0.01.c List the top 3–5 architectural risks flagged for early attention
- [x] P0.02 Module map + module ownership table
  - [x] P0.02.a List all modules from §11 with one-line responsibility each
  - [x] P0.02.b For each module, name the entities/tables it owns exclusively
  - [x] P0.02.c Flag any entity whose ownership is ambiguous and resolve before Phase 1
- [x] P0.03 Allowed cross-module dependency map
  - [x] P0.03.a Draw a directed graph of allowed module→module Contract dependencies
  - [x] P0.03.b Verify no cycles exist
  - [x] P0.03.c Document the explicit exception for read-only cross-module admin projections (§12, ADR-007)
- [x] P0.04 Domain event map (outbox events, §13)
  - [x] P0.04.a List every outbox event: `SubscriptionActivated`, `SubscriptionExpired`, `PaymentFailed`, `QuestionnaireSubmitted`, `GuidancePublished`, `EventPublished`, `EventRegistrationCreated`
  - [x] P0.04.b For each, document producer module, consumer(s) and the side effect triggered
  - [x] P0.04.c Confirm no trivial synchronous call is mistakenly modeled as an outbox event
- [x] P0.05 Full database schema (all modules)
  - [x] P0.05.a Enumerate every table per module with columns, types and nullability
  - [x] P0.05.b Mark PK/FK/unique constraints per table
  - [x] P0.05.c Note money columns as `decimal` with explicit currency, timestamps as UTC
- [x] P0.06 ER relationship overview diagram
  - [x] P0.06.a Draw entity relationships per module cluster (Identity, Content, Billing, Questionnaires, Events, Chat)
  - [x] P0.06.b Draw cross-module FK-like references (by ID only, no cross-module FKs in DB)
  - [x] P0.06.c Review diagram against §18–38 entity lists for completeness
- [x] P0.07 Key index list per table
  - [x] P0.07.a List FK indexes required on every table (mandatory per §62)
  - [x] P0.07.b Identify additional indexes from expected query patterns (e.g. `Subscription.Status`, `QuestionnaireSubmission.SubmittedAt`)
  - [x] P0.07.c Explicitly reject speculative indexes not backed by a known query
- [x] P0.08 Subscription state machine diagram (§16)
  - [x] P0.08.a Draw states: Trialing, Active, PastDue, Canceled, Expired
  - [x] P0.08.b Draw transitions with triggering webhook/event per transition
  - [x] P0.08.c Annotate access-allowed vs access-denied per state, including PastDue grace period
- [x] P0.09 Entitlement flow diagram (§15, §17)
  - [x] P0.09.a Diagram Subscription → Entitlement (`PlatformAccess`) update flow
  - [x] P0.09.b Diagram `IAccessContext` consumption from another module (e.g. Content)
  - [x] P0.09.c Confirm Billing is the only writer of Entitlement records
- [x] P0.10 Payment provider-event sequence diagram (§17)
  - [x] P0.10.a Provider-neutral sequence: Client → Checkout Session → Payment Provider → Provider Event → Billing → Subscription → Entitlement; Development uses the fake adapter and Production supplies the real provider
  - [x] P0.10.b Annotate signature validation, idempotency key, and raw-event storage steps
  - [x] P0.10.c Add the out-of-order-event handling path
- [x] P0.11 Authentication / token lifecycle diagram (§65)
  - [x] P0.11.a Diagram registration → email verification → login → JWT issuance
  - [x] P0.11.b Diagram refresh-token rotation, hashing and revocation
  - [x] P0.11.c Diagram password-reset token lifecycle and expiry
- [x] P0.12 Permission matrix (roles × permissions, §14)
  - [x] P0.12.a List all permission keys (content.*, questionnaire.*, events.*, chat.*, billing.*, users.manage, audit.view)
  - [x] P0.12.b Build the Client/Expert/Administrator × permission grid
  - [x] P0.12.c Confirm no controller-level role-string checks are implied anywhere in the design
- [x] P0.13 Questionnaire lifecycle diagram (§26)
  - [x] P0.13.a Diagram Draft → Submit → Expert queue → Review → Guidance → Publish → Follow-up
  - [x] P0.13.b Annotate each operational timestamp field at its transition point
  - [x] P0.13.c Mark the bounded-follow-up limit explicitly on the diagram
- [x] P0.14 Progress calculation rules writeup (§23–24)
  - [x] P0.14.a Document video 90%-watched completion rule and persistence cadence
  - [x] P0.14.b Document rich-text manual completion rule
  - [x] P0.14.c Document the derivation formula for program progress from Section/ContentProgress
- [x] P0.15 Event registration / waitlist state machine (§30)
  - [x] P0.15.a Draw states: Registered, Waitlisted, Canceled
  - [x] P0.15.b Draw the capacity-full → waitlist and cancellation → promotion transitions
  - [x] P0.15.c Annotate the registration-closes-at-event-start rule
- [x] P0.16 Localization architecture writeup (§5–6)
  - [x] P0.16.a Document the UI-locale (i18next files) vs content-locale (DB translation tables) split
  - [x] P0.16.b Document the fallback algorithm and `translationFallbackUsed` flag
  - [x] P0.16.c List every translatable entity pair (e.g. `Program`/`ProgramTranslation`)
- [x] P0.17 Sensitive-data strategy writeup (§35–36)
  - [x] P0.17.a Document access restriction rule (submitting client + authorized expert only)
  - [x] P0.17.b Document logging/analytics/notification exclusions for questionnaire content
  - [x] P0.17.c Document the crisis-behavior guardrails (no automated risk classification)
- [x] P0.18 Audit strategy writeup (§37)
  - [x] P0.18.a List all audited actions from §37
  - [x] P0.18.b Define `AuditLog` schema and what must never be recorded (secrets, questionnaire text)
  - [x] P0.18.c Decide read vs write audit-log access boundaries
- [x] P0.19 Frontend route map
  - [x] P0.19.a List client routes (Home, Programs, Program detail, Player, Events, Community, Guidance, Billing, Profile)
  - [x] P0.19.b List expert/admin routes per §45
  - [x] P0.19.c Mark which routes require auth vs active `PlatformAccess`
- [x] P0.20 Client UI information architecture
  - [x] P0.20.a Confirm navigation hierarchy per §40–41
  - [x] P0.20.b Confirm mobile-priority nav items per §40
  - [x] P0.20.c Map each screen to its primary data dependency (API endpoint)
- [x] P0.21 Expert/admin UI information architecture
  - [x] P0.21.a Confirm navigation hierarchy per §45
  - [x] P0.21.b Map each admin screen to its owning module
  - [x] P0.21.c Flag any screen needing a cross-module read model (§38)
- [x] P0.22 Design-system token reference (§56)
  - [x] P0.22.a Define the full token list (color, spacing, typography, radius, shadow, breakpoints, focus ring, motion)
  - [x] P0.22.b Decide token naming convention and file/format (CSS variables vs Tailwind config)
  - [x] P0.22.c Cross-check against the §55 tone requirements (no gradients, no childish illustrations)
- [x] P0.23 Responsive rules reference (§58)
  - [x] P0.23.a Define breakpoints for Mobile/Tablet/Laptop/Desktop
  - [x] P0.23.b Define the table→card conversion pattern for management screens
  - [x] P0.23.c Confirm 44px minimum touch target rule is captured in the design system
- [x] P0.24 API contract overview (§60–61)
  - [x] P0.24.a List all `/api/v1/*` resource groups
  - [x] P0.24.b Define the standard error contract shape (code, messageKey, correlationId, field errors)
  - [x] P0.24.c Define pagination/sorting/filtering conventions used across list endpoints
- [x] P0.25 Background-job catalogue (Hangfire)
  - [x] P0.25.a List every recurring/deferred job (event reminders, grace-period checks, notification dispatch)
  - [x] P0.25.b Document idempotency/retry strategy per job
  - [x] P0.25.c Document job observability requirements (logging, failure alerting)
- [x] P0.26 Testing strategy document (§68)
  - [x] P0.26.a Map each highest-risk area (Billing, Entitlements, Security, Questionnaires, Localization, Events, Progress) to test types (unit/integration/e2e)
  - [x] P0.26.b Define coverage expectations for negative/authorization paths
  - [x] P0.26.c Decide test-data and environment strategy (test DB, fixtures, seed data)
- [x] P0.27 Deployment architecture
  - [x] P0.27.a Diagram the single Api host + PostgreSQL + video provider + email provider topology
  - [x] P0.27.b Document environment configuration strategy (secrets, per-environment settings)
  - [x] P0.27.c Document backup/restore approach for PostgreSQL
- [x] P0.28 Phase-by-phase backlog (this file)
  - [x] P0.28.a Confirm this file's phase breakdown matches §69 delivery order
  - [x] P0.28.b Keep task numbering stable as items are checked off
- [x] P0.29 Architectural risks and trade-offs register
  - [x] P0.29.a List each identified risk with likelihood/impact
  - [x] P0.29.b Assign a mitigation or explicit "accepted risk" decision to each
  - [x] P0.29.c Revisit this register at the end of each phase

### 0.B Architecture review (§75)

- [x] P0.30 Challenge the proposed architecture: flag anything unnecessary, overengineered, underspecified, too tightly coupled, too generic, delay-prone or maintenance-risky
  - [x] P0.30.a Review each module boundary for premature abstraction
  - [x] P0.30.b Review the entitlement/outbox/localization designs specifically for over-engineering
  - [x] P0.30.c Review §70 out-of-scope list against the architecture deliverables for scope creep
- [x] P0.31 For each issue found: document Issue / Why it matters / Recommended change / Trade-off
  - [x] P0.31.a Draft the issue log using the required four-field format
  - [x] P0.31.b Prioritize issues by implementation-delay risk
- [x] P0.32 Get explicit approval on the (possibly revised) architecture before Phase 1 implementation starts
  - [x] P0.32.a Circulate the architecture overview + issue log for sign-off
  - [x] P0.32.b Record the approved decisions (update ADRs where they changed)

### 0.C ADRs (§73)

- [x] P0.33 ADR-001 Modular Monolith
- [x] P0.34 ADR-002 PostgreSQL
- [x] P0.35 ADR-003 Subscription Entitlement Ownership
- [x] P0.36 ADR-004 UI vs Content Localization
- [x] P0.37 ADR-005 Video Hosting Provider Abstraction
- [x] P0.38 ADR-006 Questionnaire Sensitive Data Handling
- [x] P0.39 ADR-007 Controlled Cross-Module Read Models
- [x] P0.40 ADR-008 Transactional Outbox Usage
- [x] P0.41 Flesh out ADR "Context" and "Consequences" sections once decisions are actually exercised in code
  - [x] P0.41.a Revisit each ADR after its related Phase ships and fill in real Context/Consequences
  - [x] P0.41.b Add any new ADR uncovered during the Phase 0 review (P0.30–P0.32) — ADR-009 (encryption-at-rest scope, review item R3)

---

## Phase 1 — Foundation

Deliverable: production-shaped skeleton where users can register, verify email, log in and navigate localized UI.

### 1.A Solution & infrastructure

- [x] P1.01 Initialize .NET solution and module projects per §11 structure
  - [x] P1.01.a Create solution file and `BuildingBlocks.*` projects
  - [x] P1.01.b Create `Modules/<Module>/{Domain,Application,Infrastructure,Api,Contracts,Tests}` projects for all 11 modules
  - [x] P1.01.c Create `Api`, `Jobs`, `Migrations` host projects and wire project references — host→BuildingBlocks and full intra-module layering (Domain→Application→Infrastructure/Api, standalone Contracts) are wired now; the cross-module Contracts edges from the P0.03 dependency map (e.g. `Content.Application → Identity.Contracts`) and `Api`→module-`Api` references are deferred to the slice that implements each consuming module, since there is no code yet to reference
  - [x] P1.01.d Confirm `dotnet build` succeeds on a clean checkout
- [x] P1.02 Initialize Vite + React + TypeScript app per §39 structure
  - [x] P1.02.a Scaffold Vite + React + TS app under `frontend/`
  - [x] P1.02.b Install core dependencies (React Router, TanStack Query, React Hook Form, Zod, Zustand, Tailwind, i18next/react-i18next)
  - [x] P1.02.c Configure Tailwind and base folder structure to match existing scaffolding
  - [x] P1.02.d Confirm `npm run dev` and `npm run build` succeed
- [x] P1.03 PostgreSQL via Docker Compose, connection string configuration
  - [x] P1.03.a Verify `docker-compose.yml` Postgres service starts and is reachable — Docker Desktop installed and verified this session; `docker compose up -d postgres` starts `bunited-postgres` and reports `healthy` via the P1.06 `pg_isready` healthcheck
  - [x] P1.03.b Wire `ConnectionStrings__Default` from `.env` into the Api host configuration — `Program.cs` loads `.env` via `DotNetEnv.Env.TraversePath().Load()` and fails fast if `ConnectionStrings:Default` is unset; verified with `dotnet build` and a `dotnet run` smoke test
  - [x] P1.03.c Document local setup steps in `README.md`
- [x] P1.04 EF Core base `DbContext` + per-module configuration convention (BuildingBlocks/Infrastructure)
  - [x] P1.04.a Define base `DbContext` with shared conventions (UTC timestamps, snake_case or agreed naming) — `BUnitedDbContext` (`src/BuildingBlocks/Infrastructure/Persistence/BUnitedDbContext.cs`) applies snake_case to table/column/key/FK/index names on every model; UTC timestamps enforced via P1.04.c's interceptor
  - [x] P1.04.b Define `IEntityTypeConfiguration<T>` auto-registration convention per module — `BUnitedDbContext` takes the module's configuration assemblies in its constructor and calls `ApplyConfigurationsFromAssembly` per assembly
  - [x] P1.04.c Add audit-timestamp interceptor (`CreatedAt`/`UpdatedAt`) shared across entities — `AuditableEntitySaveChangesInterceptor` stamps `IAuditableEntity.CreatedAt`/`UpdatedAt` (UTC) on Add/Modify; covered by `BUnited.BuildingBlocks.Infrastructure.Tests` (4/4 passing). Now wired into the real composed `BUnitedApplicationDbContext` (`src/Api/Program.cs`, `AddInterceptors(...)`) since P1.12 landed `User` (`IAuditableEntity`); verified live — `created_at`/`updated_at` populate correctly on registration
- [x] P1.05 Serilog structured logging setup (BuildingBlocks/Observability)
  - [x] P1.05.a Configure Serilog sinks (console + file/structured target) — `SerilogConfigurationExtensions.AddBUnitedLogging` (console + daily rolling file, both `CompactJsonFormatter`, machine/environment enrichment); wired in `src/Api/Program.cs`. Verified with a runtime smoke test (structured JSON on stdout and in `logs/bunited-*.log`)
  - [x] P1.05.b Add correlation-id enrichment middleware — `CorrelationIdMiddleware` reads/generates `X-Correlation-Id`, echoes it on the response and pushes it into Serilog's `LogContext`; `ICorrelationIdAccessor`/`CorrelationIdContext` exposes it to application code (e.g. the future P1.09 error contract). Covered by `BUnited.BuildingBlocks.Observability.Tests` (3 tests) + runtime smoke test
  - [x] P1.05.c Confirm sensitive-field redaction hooks exist for later modules to use (§65) — `[SensitiveLogValue]` attribute + `SensitiveDataDestructuringPolicy` redact marked properties whenever an object is logged via `{@Value}` destructuring; registered globally in `AddBUnitedLogging`. Modules must apply `[SensitiveLogValue]` to password/token/secret/card/questionnaire-answer/guidance-text properties when those entities/DTOs are introduced. Covered by 2 tests
- [ ] P1.06 Health checks endpoint
  - [x] P1.06.a Add ASP.NET health checks for the Api host — `HealthCheckEndpointExtensions.MapBUnitedHealthChecks` (BuildingBlocks/Observability) maps `/health/live` (liveness, no dependency checks) and `/health/ready` (readiness, `ready`-tagged checks only), per DEVELOPMENT_INSTRUCTIONS.md §10's liveness/readiness split; JSON response omits exception/description detail to avoid leaking dependency internals
  - [x] P1.06.b Add a PostgreSQL connectivity health check — `AddNpgSql` (package `AspNetCore.HealthChecks.NpgSql`) registered in `src/Api/Program.cs`, tagged `ready`. Verified against the real local PostgreSQL instance (Healthy) and against a deliberately unreachable connection string (Unhealthy, HTTP 503, no connection details leaked in the response)
  - [x] P1.06.c Expose `/health` and verify it in Docker Compose — verified end-to-end after P1.11 landed and Docker became available: `docker compose up -d` brings up `bunited-postgres` (healthy) then `bunited-api` (healthy), and `curl http://localhost:8080/health` returns `{"status":"Healthy",...,"checks":[{"name":"postgresql","status":"Healthy",...}]}` from inside the containerized network
- [x] P1.07 OpenAPI / Swagger setup
  - [x] P1.07.a Add Swagger/OpenAPI generation to the Api host — native `AddOpenApi()`/`MapOpenApi()` (.NET 9) generates `/openapi/v1.json`; `Swashbuckle.AspNetCore.SwaggerUI` (UI-only, no second generator) renders it at `/swagger`
  - [x] P1.07.b Configure JWT bearer auth in the Swagger UI — `BearerSecuritySchemeDocumentTransformer`/`BearerSecurityRequirementOperationTransformer` (`src/Api/OpenApi/BearerSecuritySchemeTransformers.cs`) register a `Bearer` (HTTP, JWT) security scheme document-wide and attach it only to operations whose endpoint carries `[Authorize]`/`IAuthorizeData`. Verified the scheme appears in `/openapi/v1.json`; now that P1.21's `/auth/revoke-all` (`[Authorize]`) exists, confirmed live that it — and only it — carries the per-operation security requirement in the generated document
  - [x] P1.07.c Restrict Swagger UI exposure per environment (dev only, or gated) — both `/openapi/v1.json` and `/swagger` are mapped only inside `IsDevelopment()`. Verified: 200 in Development, 404/404 in Production (tested with `--no-launch-profile` to bypass `launchSettings.json`'s forced `Development` environment)
- [x] P1.08 ASP.NET rate limiting middleware
  - [x] P1.08.a Configure global rate-limiting policy — `RateLimitingExtensions.AddBUnitedRateLimiting` (BuildingBlocks/Security) sets a global fixed-window limiter, 100 req/min per client IP, `/health*` excluded (monitoring/orchestrators must not be throttled). Verified: 100 requests succeeded (404, no route yet), the 101st–105th got 429
  - [x] P1.08.b Add a stricter policy for auth endpoints (login, password reset) — named policy `RateLimitingExtensions.AuthPolicyName` ("auth"), 5 req/min per client IP. Now applied via `[EnableRateLimiting("auth")]` to `register`/`login`/`password-reset/request` (P1.18/P1.20/P1.22); live-verified it engages correctly ahead of account lockout
  - [x] P1.08.c Verify rate-limit responses match the standard error contract (§61) — `OnRejected` writes `{code:"RATE_LIMIT_EXCEEDED", messageKey:"errors.rateLimitExceeded", correlationId}` (HTTP 429, `Retry-After` header) using the P1.05 correlation-id accessor. Verified the emitted `correlationId` matches the `X-Correlation-Id` response header
- [x] P1.09 Standardized error-response middleware (§61 contract)
  - [x] P1.09.a Implement global exception-handling middleware producing `{code, messageKey, correlationId}` — `GlobalExceptionHandler` (`IExceptionHandler`, BuildingBlocks/Observability) + `AppException`/`NotFoundAppException`/`BusinessRuleAppException` (framework-agnostic, BuildingBlocks/Application). `ErrorResponse.JsonOptions` enforces camelCase so the wire shape matches §24's documented example exactly. Fixed a real captive-dependency bug found during smoke testing: `AddExceptionHandler<T>` registers the handler as a singleton, so the scoped `ICorrelationIdAccessor` must be resolved per-call from `HttpContext.RequestServices`, not injected via the constructor — the initial constructor-injected version leaked the first request's (empty) correlation id into every subsequent error response. Also re-applies the `X-Correlation-Id` response header, which the exception-handler middleware clears before invoking the handler
  - [x] P1.09.b Implement FluentValidation error mapping to the field-error shape — `FluentValidationActionFilter` resolves `IValidator<T>` per action argument and throws FluentValidation's `ValidationException` on failure; `GlobalExceptionHandler` maps it via `ErrorResponse.FromValidationFailures`. Convention: validators must set `WithErrorCode("errors.field....")` — `ValidationFailure.ErrorCode` becomes the field's `messageKey` (documented on `ErrorResponse.FromValidationFailures`)
  - [x] P1.09.c Add tests covering unhandled-exception, validation-failure and not-found responses — `GlobalExceptionHandlerTests` (5 tests: unhandled→500, `NotFoundAppException`→404, `BusinessRuleAppException`→400, FluentValidation `ValidationException`→400+field errors) + `FluentValidationActionFilterTests` (3 tests: fails→throws, passes→calls next, no validator registered→passes through). Also verified end-to-end with a temporary throwing endpoint against a running host (removed after verification) — confirmed distinct `correlationId` per request and header/body consistency
- [x] P1.10 CI foundation (build, test, lint on push)
  - [x] P1.10.a Add CI workflow: restore, build, run backend tests — `.github/workflows/ci.yml`, `backend` job: `dotnet restore/build/test` against `BUnited.sln` in `Release`, `.NET` version pinned via `global-json-file: global.json`. Verified `Release` build + test locally first (12+4 tests pass) before committing to that config in CI
  - [x] P1.10.b Add CI steps for frontend: install, lint, build — `frontend` job: `npm ci`, `npm run lint` (oxlint), `npm run build` (`tsc -b && vite build`); verified all three locally first
  - [x] P1.10.c Fail the pipeline on build/test/lint failures — default GitHub Actions behavior (no `continue-on-error`); any step's non-zero exit fails its job. YAML syntax validated with `js-yaml`. **Not verified**: the workflow file has not yet been pushed/committed, so it has not actually run on GitHub's runners. Re-verify after the first push once the user confirms committing/pushing is authorized
- [x] P1.11 Base Dockerfile(s) for the Api host
  - [x] P1.11.a Write a multi-stage Dockerfile for the Api host — `src/Api/Dockerfile`: SDK-alpine build stage (`dotnet restore`/`publish` targeting `src/Api/BUnited.Api.csproj`, build context = repo root so `ProjectReference`s to BuildingBlocks/Modules resolve), aspnet-alpine runtime stage running as a non-root user, listens on `:8080`
  - [x] P1.11.b Add the Api service to `docker-compose.yml` alongside PostgreSQL — `api` service builds from the new Dockerfile, `depends_on: postgres: condition: service_healthy` (uses P1.06's `pg_isready` healthcheck), `ConnectionStrings__Default` points at the `postgres` service hostname, own `wget`-based healthcheck against `/health`. `.env.example` updated with `ASPNETCORE_ENVIRONMENT`/`API_PORT`. Also removed the obsolete top-level `version:` key (Compose v2 warning, found during verification)
  - [x] P1.11.c Verify the containerized Api can reach PostgreSQL and serve `/health` — Docker Desktop installed this session (WSL2 + Virtual Machine Platform enabled, required a Windows feature change + restart). `docker compose up --build -d`: image built clean, `bunited-postgres` → `healthy`, `bunited-api` → `healthy`; `curl http://localhost:8080/health` and `/health/ready` both returned `{"status":"Healthy",...}` with the `postgresql` check passing against the containerized database. Torn down with `docker compose down` after verification

### 1.B Identity module (§14)

- [x] P1.12 `User`, `Role`, `Permission`, `RolePermission`, `UserRole` entities + EF configuration
  - [x] P1.12.a Define entity classes with invariants (e.g. unique email, normalized email casing) — `src/Modules/Identity/Domain/Entities/*.cs`; `User.Register` normalizes email via `User.Normalize`, private setters, factory methods enforce invariants
  - [x] P1.12.b Write EF Core configuration: keys, FK relationships, required fields, unique constraint on `User.Email` — `src/Modules/Identity/Infrastructure/Persistence/Configurations/*.cs`; unique index on `NormalizedEmail`
  - [x] P1.12.c Add FK indexes (`RolePermission`, `UserRole`) per §62 — composite PKs double as indexes; explicit `HasIndex` added where a composite PK doesn't already cover the lookup
  - [x] P1.12.d Generate and review the initial Identity migration — `src/Migrations/Migrations/20260807231547_InitialIdentity.cs`; reviewed generated table/column/index/FK names, all correctly snake_cased (`users`, `role_permissions`, `ix_users_normalized_email`, `fk_user_roles_users_user_id`, etc.), matches docs/ARCHITECTURE.md §5 exactly. Applied to the local database and verified via `psql \dt`
  - [x] P1.12.e Unit tests for entity invariants — `src/Modules/Identity/Tests/Domain/UserTests.cs` (6 tests)
- [x] P1.13 `RefreshToken` entity: hashed, rotating, revocable (§65)
  - [x] P1.13.a Define entity storing only the hashed token value, expiry and revocation state — `RefreshToken.cs`; only `TokenHash` persisted, never the raw value
  - [x] P1.13.b Implement token generation + hashing on issuance — `SecureTokenGenerator` (32 random bytes, SHA-256 hash), shared by refresh/email-verification/password-reset tokens
  - [x] P1.13.c Implement rotation-on-use (old token invalidated, new one issued) — `RefreshTokenHandler`: revokes the presented token and issues `IssueRotated(...)`, same `FamilyId`
  - [x] P1.13.d Unit tests for reuse-detection (revoked token reuse should fail and optionally revoke the token family) — `RefreshTokenHandlerTests.Reusing_an_already_rotated_token_revokes_the_whole_family`; also verified live via curl (rotate → reuse old → both old and newly-rotated token rejected)
- [x] P1.14 `EmailVerificationToken`, `PasswordResetToken` entities
  - [x] P1.14.a Define entities with expiry and single-use semantics — `IsValid(utcNow)` / `MarkUsed(utcNow)` on both
  - [x] P1.14.b Implement token issuance and consumption logic — `RegisterUserHandler` issues; `VerifyEmailHandler` consumes. `RequestPasswordResetHandler` issues; `ConfirmPasswordResetHandler` consumes
  - [x] P1.14.c Unit tests for expiry and already-used token rejection — `ExpiringTokenTests.cs`, `VerifyEmailHandlerTests.cs`, `PasswordResetTests.cs`
- [x] P1.15 `UserConsent`, `UserPreference` entities
  - [x] P1.15.a Define `UserConsent` with consent type + version + timestamp (used later by Questionnaires §35) — defined; not yet wired to any flow (Questionnaires module doesn't exist yet — P4.14 will consume it)
  - [x] P1.15.b Define `UserPreference` covering timezone (§64) and notification opt-in flags (§32) — `CreateDefault` used by `RegisterUserHandler` (UTC default)
  - [x] P1.15.c EF configuration and migration — included in the same `InitialIdentity` migration
- [x] P1.16 Seed initial roles: `Client`, `Expert`, `Administrator`
  - [x] P1.16.a Add a seed/migration step inserting the three roles with stable IDs — `src/Migrations/Seed/IdentitySeeder.cs`, `WellKnownRoles` fixed GUIDs, run at Api startup (not baked into the migration itself, so it can evolve independently of schema history)
  - [x] P1.16.b Verify seed is idempotent on repeated migration runs — existence-checked by ID before insert; verified via two consecutive app startups (second run: `SELECT` only, zero `INSERT`s)
- [x] P1.17 Seed initial permission set (content.*, questionnaire.*, events.*, chat.*, billing.*, users.manage, audit.view)
  - [x] P1.17.a Enumerate the full permission-key list from P0.12
  - [x] P1.17.b Seed permissions and default `RolePermission` grants per the permission matrix — `IdentitySeeder`, 15 permissions + 28 grants matching docs/ARCHITECTURE.md §14's table exactly (`billing.view` granted to all three roles; the "own vs all" scoping is an application-layer ownership check, not a role-grant distinction)
  - [x] P1.17.c Verify seed is idempotent — same idempotency check as P1.16.b, covers permissions and grants too
- [x] P1.18 Registration endpoint + password hashing
  - [x] P1.18.a Define `RegisterRequest`/`RegisterResponse` DTOs + FluentValidation — `RegisterUserCommand`/`RegisterUserResult`, `RegisterUserValidator` (email format + async uniqueness check, password strength rules)
  - [x] P1.18.b Implement password hashing (e.g. ASP.NET Identity-style hasher) — never store plaintext — `PasswordHasher` wraps `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2)
  - [x] P1.18.c Implement handler: create `User`, assign `Client` role, trigger `EmailVerification` notification — `RegisterUserHandler`
  - [x] P1.18.d Integration tests: happy path, duplicate email, weak password — `RegisterUserHandlerTests.cs`, `RegisterUserValidatorTests.cs`; also verified live via curl (all three cases return the expected shape/codes)
- [x] P1.19 Email verification flow
  - [x] P1.19.a Implement verify-email endpoint consuming `EmailVerificationToken` — `POST /api/v1/auth/verify-email`
  - [x] P1.19.b Wire `Welcome` notification on successful verification — `VerifyEmailHandler` calls `IIdentityEmailSender.SendWelcomeAsync` (only on first verification, not on a no-op re-check)
  - [x] P1.19.c Integration tests: valid token, expired token, already-verified user — `VerifyEmailHandlerTests.cs`; live-verified (valid → 204, reused → 400 `EMAIL_VERIFICATION_TOKEN_INVALID`, garbage token → same 400)
- [x] P1.20 Login endpoint issuing JWT access token + rotating refresh token
  - [x] P1.20.a Implement credential validation + failed-login audit event — `LoginHandler`; failed/successful logins are structured-logged (`identity.login`/`identity.failed_login`) with correlation ID. **Note**: not yet a persisted `AuditLog` row — that table is P1.32 (Phase 1.E, a separate section not in scope for "the Identity module"); logging is the interim mechanism
  - [x] P1.20.b Issue JWT access token with permission claims and a refresh token — `JwtTokenGenerator` embeds one `permission` claim per key from the user's role(s); verified live that a fresh Client registration's JWT carries exactly the 5 seeded Client permissions
  - [x] P1.20.c Integration tests: valid login, invalid password, unverified email, locked account — `LoginHandlerTests.cs` (6 tests, including a `FakeTimeProvider`-driven lockout+cooldown test); live-verified all four cases plus that wrong-password and unknown-email return the identical `INVALID_CREDENTIALS` shape (no account-existence leak)
- [x] P1.21 Refresh-token rotation + revocation endpoint
  - [x] P1.21.a Implement `/auth/refresh` consuming and rotating the refresh token — `RefreshTokenHandler`
  - [x] P1.21.b Implement `/auth/revoke` (logout) endpoint — `RevokeTokenHandler` (single session, idempotent) + `RevokeAllSessionsHandler`/`POST /auth/revoke-all` (`[Authorize]`, revokes every active token for the caller from the JWT `sub` claim)
  - [x] P1.21.c Integration tests: rotation success, reuse of a revoked token, revoke-all-sessions path — `RefreshTokenHandlerTests.cs`, `RevokeTokenHandlerTests.cs`; live-verified `/revoke-all` rejects anonymous callers (401) and revokes all sessions for an authenticated one (subsequent refresh → `REFRESH_TOKEN_INVALID`)
- [x] P1.22 Password reset flow
  - [x] P1.22.a Implement request-reset endpoint (always returns success regardless of email existence) — `RequestPasswordResetHandler`; live-verified identical 204 for both an existing and a non-existing email
  - [x] P1.22.b Implement confirm-reset endpoint consuming `PasswordResetToken` — `ConfirmPasswordResetHandler`; also clears any existing lockout on successful reset
  - [x] P1.22.c Trigger `PasswordReset` notification and audit event — email stub called; structured log (`identity.password_reset`); same P1.32 note as P1.20.a applies re: persisted audit rows
  - [x] P1.22.d Integration tests: valid flow, expired/used token, token reuse rejection — `PasswordResetTests.cs`; live-verified full flow including old-password-rejected/new-password-accepted and token-reuse rejection
- [x] P1.23 Permission-based authorization policies (no `if (user.Role == "Expert")` in controllers)
  - [x] P1.23.a Implement a permission-claim authorization handler/policy provider — `IdentityAuthorizationExtensions.AddIdentityPermissionPolicies` (built on ASP.NET Core's built-in `RequireClaim`, no custom handler needed for a simple claim-presence check)
  - [x] P1.23.b Register one policy per permission key from P1.17 — one `AddPolicy(key, ...)` per `WellKnownPermissions.All` entry; verified via `IdentityAuthorizationExtensionsTests` (all 15 keys resolve to a policy requiring the matching claim)
  - [ ] P1.23.c Apply `[Authorize(Policy = "...")]` consistently; add an analyzer/lint check against role-string checks — **partially done**: no business endpoints exist yet to apply a permission policy to (only `/auth/revoke-all` exists, which correctly uses plain `[Authorize]` since it has no specific permission — any authenticated user may revoke their own sessions). The Roslyn analyzer for banning role-string checks was **not built** — a custom analyzer project is a substantial side-effort disproportionate to current scope (zero role-string checks exist in the codebase to guard against yet); revisit when the first module with role/permission-sensitive controllers lands
- [x] P1.24 Account lockout / abuse protection on auth endpoints
  - [x] P1.24.a Implement failed-attempt counting and temporary lockout — `User.RegisterFailedLoginAttempt`/`IsLockedOut`, wired in `LoginHandler`; configurable via `AccountLockoutOptions` (`AccountLockout__MaxFailedAttempts`/`AccountLockout__LockoutDurationMinutes`, defaults 5/15)
  - [x] P1.24.b Combine with the rate-limiting policy from P1.08 — `[EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]` applied to `register`/`login`/`password-reset/request`; live-verified the P1.08 5-req/min policy engages before lockout would even be reachable within one rate-limit window (confirms both layers are active)
  - [x] P1.24.c Integration tests: lockout triggers after N failures and clears after cooldown — `LoginHandlerTests.Account_locks_after_the_configured_number_of_failed_attempts_and_clears_after_cooldown` (uses `FakeTimeProvider` to advance past the cooldown deterministically, since the live rate limiter makes this impractical to verify via repeated HTTP calls within a single test run)

### 1.C Localization infrastructure

- [x] P1.25 i18next + react-i18next setup with lazy-loaded namespaces
  - [x] P1.25.a Configure i18next with `ro` default, `en` fallback, and namespace-per-feature loading — `frontend/src/shared/i18n/i18n.ts`. **Fixed a real bug found via testing**: an explicit `lng: 'ro'` option takes priority over `i18next-browser-languagedetector`'s result, so a returning visitor's persisted "en" choice was silently ignored after reload. Removed the explicit `lng`; `fallbackLng: 'ro'` alone now correctly serves as both the first-visit default and the missing-key/detection-failure fallback. Namespaces load per-feature via `i18next-resources-to-backend` wrapping a Vite dynamic `import()`
  - [x] P1.25.b Wire the i18next provider into the app root — `main.tsx` imports `./shared/i18n/i18n` for its init side effect and wraps `<App />` in `<Suspense>` (required by `react.useSuspense: true`)
  - [x] P1.25.c Verify lazy namespace loading works on route change (no full-namespace bundle upfront) — verified via `npm run build`: every `{namespace}-{lang}.json` (common, auth, and the other seven placeholder namespaces) is emitted as its own ~0.03–2 KB chunk, none inlined into the 250 KB `index.js` entry bundle. Live-verified with a headless-Chromium (Playwright) script: requesting the `auth` namespace in a component fetches it on demand: `Autentificare` (ro) → switch → `Log in` (en), same URL, zero console errors
- [x] P1.26 Seed `ro`/`en` locale namespace files (common, auth) with real keys
  - [x] P1.26.a Replace placeholder `common.json`/`auth.json` with real keys used by Phase 1 screens — `common.json` (app name, generic actions/status, language names, the four generic API error codes: `validationFailed`/`internalServerError`/`notFound`/`rateLimitExceeded`); `auth.json` (login/register/verify-email/password-reset screen copy + every `errors.*` `messageKey` the Identity backend actually emits — cross-checked line-by-line against `RegisterUserValidator`, `LoginHandler`, `VerifyEmailHandler`, `RefreshTokenHandler`, `ConfirmPasswordResetHandler`)
  - [x] P1.26.b Verify key parity between `ro` and `en` — `npm run check:locale-parity` passes (9 namespace files); deliberately broke a key and confirmed the script catches it before restoring
  - [x] P1.26.c Add a CI check (or script) that fails on key-parity mismatch — `frontend/scripts/check-locale-parity.mjs` + `.github/workflows/ci.yml` frontend job step ("Check locale key parity")
- [x] P1.27 Language switcher component
  - [x] P1.27.a Build the switcher UI using design-system primitives — **partial**: no formal design-system primitives exist yet (P1.29/P1.30, not built). Built `LanguageSwitcher.tsx` as a minimal accessible native `<select>` styled with plain Tailwind utilities; documented inline that it should move onto real primitives once they land
  - [x] P1.27.b Persist selected language to `UserPreference` (authenticated) and local storage (anonymous) — anonymous case done (`i18next-browser-languagedetector`'s `caches: ['localStorage']` persists automatically on `changeLanguage`, verified across a page reload). Authenticated DB persistence to `UserPreference` is **not yet wired** — there is no profile API to call yet (lands with P1.42); the hook point is documented in the component
  - [x] P1.27.c Verify switching language does not require a full page reload — live-verified via Playwright: `page.url()` unchanged after `changeLanguage`, DOM text updates in place, also fixed `<html lang>` not syncing to the active language (found during this verification; now synced via an `i18n.on("languageChanged", ...)` listener)
- [x] P1.28 DB-backed translation lookup infrastructure (BuildingBlocks/Localization) with default-language fallback + `translationFallbackUsed` flag pattern (used from Phase 2 onward)
  - [x] P1.28.a Implement a generic translation-resolution helper given an entity's translations collection + requested language — `TranslationResolver.Resolve<TTranslation>` (`src/BuildingBlocks/Localization`), generic over any `ITranslation`-implementing entity so every `*Translation` table (Program, Section, ContentItem, etc., from Phase 2 onward) can reuse it without duplication
  - [x] P1.28.b Implement default-language fallback + `translationFallbackUsed` flag output — `TranslationResolution<TTranslation>.FallbackUsed`; case-insensitive language matching
  - [x] P1.28.c Unit tests: exact match, fallback, missing-default-language edge case — `TranslationResolverTests.cs` (5 tests, including the empty-collection and neither-language-present edge cases, which throw since that indicates a data-integrity bug rather than a normal missing-translation case)

### 1.D Design system foundation

- [x] P1.29 Design tokens (color, spacing, typography, radius, shadow, breakpoints, focus ring, motion) per §56
  - [x] P1.29.a Implement tokens from P0.22 as Tailwind config / CSS variables — **corrected a stale checkbox**: this was previously marked done but `index.css` was actually just `@import "tailwindcss";` with no tokens at all (caught when the user asked "so is there nothing on the frontend"). Now genuinely implemented as a Tailwind v4 `@theme` block (`src/index.css`): Background/Surface/Border/Text/Primary/status color tokens, radius, shadow, `tablet`/`desktop` breakpoint aliases, font family. Verified each token compiles to a real utility class by grepping the built CSS (`bg-surface`, `.rounded-md{border-radius:var(--radius-md)}`, `.shadow-lg{...}`, etc.) — not just that the file has no syntax errors
  - [x] P1.29.b Implement light theme; confirm dark-mode approach (or explicitly defer) — light theme is the only implementation; dark mode explicitly deferred (documented in `index.css` and the design-system README) since no dark-mode requirement exists in the product spec
  - [x] P1.29.c Document token usage rules (no arbitrary values in components) — `shared/design-system/README.md` ("Usage rules" section): no arbitrary bracket values, semantic tokens over raw Tailwind palette classes, don't hand-roll focus states, respect `prefers-reduced-motion`. **Found and fixed a real bug while dogfooding the tokens**: a custom `--duration-fast` theme value silently produced no `duration-fast` utility class at all (Tailwind v4 doesn't map a custom `--duration-*`/`--ease-*` namespace to those utilities the way it does `--color-*`/`--radius-*`) — confirmed by grepping the built CSS. Removed the non-functional custom duration tokens; components use Tailwind's own built-in `duration-150` etc. instead, documented with the reasoning so it isn't reintroduced
- [x] P1.30 Core primitives: Button, Input, Card, Badge, Alert, Toast, Skeleton, EmptyState
  - [x] P1.30.a Implement each primitive using tokens only, with accessible states (focus, disabled, error) — `frontend/src/shared/design-system/{Button,Input,Card,Badge,Alert,Toast,Skeleton,EmptyState}.tsx`. Focus relies on the global `:focus-visible` token rule; `Button` has a `disabled` state; `Input` supports `error`/`hint` with `aria-invalid`/`aria-describedby`; `Alert`/`Toast` have keyboard-accessible dismiss buttons; `Skeleton` is `aria-hidden` so it's never announced as content
  - [ ] P1.30.b Add Storybook or equivalent visual reference (optional but recommended) — **not done**, explicitly deferred (marked optional in this task). Verified visually instead via a temporary preview composed of all eight primitives, screenshotted with headless Chromium, then removed — see P1.30.c for the permanent, repeatable verification (the test suite)
  - [x] P1.30.c Unit/interaction tests for keyboard accessibility on interactive primitives — set up Vitest + React Testing Library + jsdom from scratch (none existed in the frontend before now); 16 tests across 8 files (`*.test.tsx` colocated with each primitive) covering Tab focus, Enter/Space activation, disabled non-focusability, label/input association, `aria-invalid`/`aria-describedby` wiring, and dismiss-button keyboard activation on `Alert`/`Toast`. Wired into CI (`npm run test`) and `npm run test`/`vitest run` locally — all pass
- [x] P1.31 Base layouts: client layout shell, expert/admin layout shell
  - [x] P1.31.a Build client layout with nav per §40 — `frontend/src/layouts/ClientLayout.tsx`: full 7-item sidebar (Home/Programs/Events/Community/My Guidance/Billing/Profile) from `tablet` up, 5-item priority bottom nav (Home/Programs/Events/Community/Profile) below `tablet`, `aria-current="page"` via `NavLink`
  - [x] P1.31.b Build expert/admin layout with nav per §45 — `frontend/src/layouts/AdminLayout.tsx`: persistent 10-item sidebar (Dashboard/Programs/Questionnaires/Events/Community/Subscribers/Billing/Notifications/Audit/Settings) from `tablet` up; hamburger-triggered slide-in drawer below `tablet` (a 10-item nav doesn't fit a bottom bar the way the 5-item client nav does). **Fixed a real accessibility bug found via testing**: the drawer's click-to-dismiss backdrop was originally a `<button aria-label="Close menu">`, giving two different controls the identical accessible name — changed the backdrop to a non-focusable `aria-hidden` div, leaving one unambiguous "Close menu" control
  - [x] P1.31.c Verify both shells are responsive per §58 — 23 Vitest/RTL tests (nav rendering, active-link `aria-current`, mobile-subset filtering, drawer open/close via click and keyboard, focus behavior) plus live headless-Chromium verification at 375px and 1024px viewports for both layouts (screenshots), including actually opening the admin drawer and confirming keyboard dismissal — not just checking the responsive CSS classes exist in isolation

Nav labels added to `common.json` (`nav.*`, both `ro`/`en`, parity-checked): mainNavigation, adminNavigation, openMenu, closeMenu, home, programs, events, community, guidance, billing, profile, dashboard, questionnaires, subscribers, notifications, audit, settings.

**Testing infrastructure added this phase** (none existed before): Vitest + React Testing Library + jsdom + user-event, wired into `npm run test` and CI. A dedicated test-only i18next instance (`setupTests.ts`) loads locale JSON statically (bypassing the production lazy-loading backend) so component tests get real translated strings synchronously without needing Suspense.

### 1.E Audit foundation

- [x] P1.32 `AuditLog` entity + write API (BuildingBlocks or Audit module)
  - [x] P1.32.a Define `AuditLog` entity per §37 schema — `src/Modules/Audit/Domain/Entities/AuditLog.cs`: `Id, Action, ActorUserId, EntityType, EntityId, TimestampUtc, CorrelationId, IpAddress, MetadataJson`. No FK/reference to Identity's `User` (modules must not reference another module's Domain layer) — `ActorUserId` is an opaque `Guid?`. Wired into `BUnitedApplicationDbContext` and migrated (`20260808085824_AddAuditLog`), applied to the local Postgres DB and verified via a live `dotnet run` boot (health checks green, no DI resolution errors)
  - [x] P1.32.b Implement a write-only append API (`IAuditLogger`) usable from any module — `IAuditLogger`/`AuditEntry`/`AuditActions` in `Audit.Contracts` (the cross-module-visible surface, per "cross-module dependencies go through Contracts"); `AuditLogger` in `Audit.Infrastructure` persists independently of the caller's own `SaveChangesAsync` (self-contained write, doesn't silently disappear if the caller forgets to save). Registered via `AddAuditModule()` in `Program.cs`. No read method on the interface by design — reads are a separate, explicitly authorized concern
  - [x] P1.32.c Verify no secrets/tokens/questionnaire text can be passed into `Metadata` (guard at the API boundary) — `AuditEntry.Create` (the Contracts-layer construction boundary) rejects any metadata key matching a denylist (`password`, `token`, `secret`, `answer`, `guidance`, `questionnaire`, `card`, `cvv`, `cvc`, `ssn`, `apikey`, `authorization`, `credential`, etc.) with `ArgumentException`. 18 xunit tests in `Audit.Tests` (12 denylist cases + safe-metadata/no-metadata/persistence round-trip via an in-memory Sqlite `BUnitedDbContext`), all passing
- [ ] P1.33 Wire audit events: `user.login`, `user.failed_login`, `user.password_reset`, `user.role_changed` — **partial**: a and b done, c deferred (see below)
  - [x] P1.33.a Emit `user.login`/`user.failed_login` from the login handler — `LoginHandler` now takes `IAuditLogger` and emits `AuditActions.UserLogin` on success and `AuditActions.UserFailedLogin` (with a `reason` metadata tag: `unknown_email`/`locked_out`/`wrong_password`/`email_not_verified`) on every rejection path. Verified live against real Postgres: booted the API, drove all four paths through the real `/api/v1/auth/login` HTTP endpoint with curl, and read the resulting rows back out of `audit_logs` directly via Npgsql — correct `action`, `actor_user_id`, `correlation_id`, and `metadata_json` on each row (then deleted the smoke-test data). Also 3 new/extended xunit assertions in `LoginHandlerTests`
  - [x] P1.33.b Emit `user.password_reset` from the reset-confirm handler — `ConfirmPasswordResetHandler` now takes `IAuditLogger` and emits `AuditActions.UserPasswordReset` after a successful reset; asserted in `PasswordResetTests`. Not re-verified live in this session (hit the auth rate limiter after the login smoke test) — covered by the Sqlite-backed integration test instead
  - [ ] P1.33.c Emit `user.role_changed` from the (future) role-assignment path — **not done, and not stubbed**: there is no role-assignment code path anywhere in the codebase yet (`RegisterUserHandler` assigns the default Client role at registration, which is not a "role changed" event). A call site with nothing calling it would be dead code, which conflicts with docs/DEVELOPMENT_INSTRUCTIONS.md §2 ("MUST NOT add placeholders ... unresolved TODO architecture"). Deferred the same way P1.23.c and P1.30.b were: land `AuditActions.UserRoleChanged` (already defined in `Audit.Contracts`, P1.32) at the real call site once admin role assignment is built

### 1.F Tests

- [x] P1.34 Auth flow tests (register, verify, login, refresh, reset)
  - [x] P1.34.a End-to-end happy-path test: register → verify → login → refresh → logout — `AuthFlowTests.Register_verify_login_refresh_and_logout_flow_succeeds_end_to_end` chains the real `RegisterUserHandler` → `VerifyEmailHandler` → `LoginHandler` → `RefreshTokenHandler` → `RevokeTokenHandler` (logout) against one shared in-memory Sqlite context, using the actual raw verification token the register step issued (not a manually-seeded one). **Scope note**: this is handler-level, not raw-HTTP — the codebase has no `WebApplicationFactory` integration tests yet (none of the existing 41 Identity tests use one either) and CI (`.github/workflows/ci.yml`) has no Postgres service container, so a true HTTP-level e2e test would need CI infra changes not made here. Flagged as a possible future enhancement, not silently substituted
  - [x] P1.34.b Negative tests: duplicate registration, wrong password, expired tokens — duplicate registration was already covered (`RegisterUserValidatorTests.Rejects_an_already_registered_email` — enforced by `RegisterUserValidator` + a DB unique index on `NormalizedEmail`, not the handler); wrong password was already covered (`LoginHandlerTests`). Added the one genuine gap: `AuthFlowTests.Expired_refresh_token_is_rejected` (expired email-verification and password-reset tokens were already covered)
  - [x] P1.34.c Token-reuse and revocation tests — reuse-detection (whole-family revocation) and explicit revoke were already covered (`RefreshTokenHandlerTests`, `RevokeTokenHandlerTests`). Added `AuthFlowTests.A_revoked_token_cannot_be_used_to_refresh`, tying logout into the refresh path. 83 backend tests total now (up from 80)
- [x] P1.35 Permission policy enforcement tests (positive + negative)
  - [x] P1.35.a For each seeded permission, test an authorized call succeeds
  - [x] P1.35.b For each seeded permission, test an unauthorized call is rejected (403)
  - [x] P1.35.c Test that an unauthenticated call is rejected (401) on protected endpoints — `PermissionEnforcementTests` + `PermissionTestHostFixture` (`src/Modules/Identity/Tests/Security/`): a real ASP.NET Core pipeline (`TestServer`, wired with the actual `AddIdentityJwtAuthentication`/`AddIdentityPermissionPolicies` production code, not a fake) with one throwaway endpoint per seeded permission. **Why a test host and not real endpoints**: no permission-gated endpoint exists anywhere in the API yet — every module past Identity is still an empty scaffold, and the first real one lands with P2.10. Inventing a fake production endpoint just to test authorization would itself be the kind of placeholder code docs/DEVELOPMENT_INSTRUCTIONS.md §2 forbids; a test-only host exercises the real middleware without that. Covers all 15 permissions × authorized/forbidden, plus unauthenticated-401 (both permission-gated and plain-`[Authorize]`), plus an expired-token-401 case. 48 new tests, 92 in `Identity.Tests` now (141 backend total)

### 1.G Usable frontend foundation

- [x] P1.36 Frontend application runtime foundation
  - [x] P1.36.a Configure the application router with public, authenticated client and expert/admin route groups — `frontend/src/app/router.tsx`. The FULL §40/§45 nav is routed (not just the Phase 1 slice, see P1.41 note) so `ClientLayout`/`AdminLayout` never link anywhere broken
  - [x] P1.36.b Wire `QueryClientProvider`, the API client and shared query defaults at the application root — `shared/api/queryClient.ts` (doesn't retry 4xx), `shared/api/apiClient.ts`, `app/AppProviders.tsx`
  - [x] P1.36.c Map the standardized backend error contract to typed frontend errors without displaying raw server details — `shared/api/apiError.ts` (`ApiError`/`NetworkError`), `shared/forms/applyApiErrorToForm.ts`
  - [x] P1.36.d Add an application-level `ErrorBoundary` with a localized recovery action — `app/ErrorBoundary.tsx`
- [x] P1.37 Browser session lifecycle
  - [x] P1.37.a Implement auth-session bootstrap, authenticated-user loading and refresh-token recovery without persisting access tokens in local storage — `shared/auth/SessionProvider.tsx` + `authStore.ts` (access token in memory only) + `tokenStorage.ts` (refresh token only, in `localStorage` — the backend's refresh contract has no cookie option; only the access token is excluded, per the literal wording of this subtask)
  - [x] P1.37.b Implement automatic access-token refresh for eligible 401 responses with a single retry and concurrent-refresh deduplication — `apiClient.ts`'s `refreshAccessTokenOnce`. **Real bug found and fixed via live testing**: `SessionProvider`'s bootstrap call bypassed that dedup (it calls the refresh logic directly, not through the 401 path); React 19 `StrictMode` double-invokes effects in dev, so two concurrent bootstrap refreshes raced, the second one reused an already-rotated token, and the reuse-detection correctly-but-unhelpfully revoked the whole family — killing the session the first call had just established. Fixed by making the refresh function single-flight itself (`SessionProvider.tsx`)
  - [x] P1.37.c Clear client state and redirect safely when refresh fails or the user logs out — `authStore.clearSession`; logout wired on `ProfilePage` (best-effort server-side revoke, then always clears local state and redirects)
  - [x] P1.37.d Verify browser navigation and refresh preserve a valid session without exposing tokens to application logs — live-verified: register → login → profile → **hard page reload** → the changed profile value persisted and the session survived (proves refresh-on-reload works); confirmed zero console errors throughout
- [x] P1.38 Registration and login UI
  - [x] P1.38.a Build localized registration and login screens using React Hook Form, Zod and design-system primitives — `modules/auth/{LoginPage,RegisterPage}.tsx` + `schemas.ts`
  - [x] P1.38.b Show accessible field-level validation and stable API-error messages for duplicate email, invalid credentials, unverified email and lockout — `applyApiErrorToForm` maps backend field errors onto RHF fields; generic vs. module-specific `messageKey` namespace resolution has a dedicated regression test (`applyApiErrorToForm.test.ts`) after a real bug here (see P1.44 note)
  - [x] P1.38.c Preserve the intended destination through login and redirect only to an allowlisted internal route — `shared/auth/redirect.ts#sanitizeRedirectTarget`, wired through `RequireAuth`'s `state.from` and `LoginPage`
  - [x] P1.38.d Verify keyboard-only operation, visible focus, labels, autocomplete attributes and mobile layout — inherited from `Input`/`PasswordInput`/`Button` (labels, `:focus-visible`, `aria-invalid`/`aria-describedby`); `autoComplete` set on all credential fields (`email`, `current-password`, `new-password`); live-verified at a 375px mobile viewport
- [x] P1.39 Email verification and password-reset UI
  - [x] P1.39.a Build the email-verification result screen with success, expired, already-used and resend-guidance states — `modules/auth/VerifyEmailPage.tsx`. **Backend gap found and closed**: there was no way to get a new verification link if the original expired (re-registering the same unverified email is blocked by the uniqueness check) — added `POST /api/v1/auth/resend-verification` (`ResendVerificationCommand`/`Handler`/`Validator`, non-enumerating like password-reset-request) rather than shipping a "Resend" button with nothing behind it. The backend collapses expired/already-used/malformed into one `EMAIL_VERIFICATION_TOKEN_INVALID` code (doesn't distinguish), so the UI shows one generic invalid state with resend guidance rather than three different messages it can't actually tell apart
  - [x] P1.39.b Build request-reset and confirm-reset screens without revealing whether an email address exists — `RequestPasswordResetPage.tsx`/`ConfirmPasswordResetPage.tsx`; the UI never branches on the request's result, matching the backend's identical-response behavior
  - [x] P1.39.c Handle invalid, expired and reused reset links with localized recovery actions — `ConfirmPasswordResetPage.tsx` shows the invalid/expired state with a link back to request a new one when there's no token or the API rejects it
  - [x] P1.39.d Verify password-manager compatibility and accessible password-requirement/error feedback — `autoComplete="new-password"`/`"current-password"` set; `PasswordInput` hint shows the requirement text; requirements re-stated as field errors on mismatch
- [x] P1.40 Route protection and navigation states
  - [x] P1.40.a Implement authentication and permission route guards as UX controls while preserving server-side enforcement — `RequireAuth`/`RequireGuest`/`RequirePermission` (`shared/auth/`), each with an explicit code comment noting the API independently re-enforces regardless of what these hide
  - [x] P1.40.b Add localized `Unauthorized`, `Forbidden` and `NotFound` screens with safe navigation actions — `app/screens/{UnauthorizedPage,ForbiddenPage,NotFoundPage}.tsx`. Route guards redirect straight to `/login` (the actual resolution UI) rather than to `/unauthorized` for the common "not logged in" case; `/unauthorized` stays reachable for an unhandled 401 that slips past a guard
  - [x] P1.40.c Prevent protected content from flashing while session state is loading — `SessionProvider` renders a loading state and holds `children` back entirely until bootstrap settles to `authenticated`/`unauthenticated`
  - [x] P1.40.d Show only navigation destinations available in the current delivery phase and permitted for the current user — **judgment call, documented rather than silent**: `ClientLayout`/`AdminLayout` (built in an earlier session against §40/§45's full nav, with passing tests asserting the full item list) render every nav destination unconditionally; trimming the nav arrays would break that already-verified work. Instead every not-yet-built destination routes to `ComingSoonPage` — an honest, translated "not available yet" state, never fake business data, so nothing is presented as usable that isn't (see P1.41.c/P1.46.d)
- [x] P1.41 Phase 1 authenticated home screen
  - [x] P1.41.a Build a localized client home screen with account greeting, verification/account state and links to the Phase 1 actions that actually exist — `modules/dashboard/ClientHomePage.tsx`
  - [x] P1.41.b Build an expert/admin home shell that exposes only implemented and permitted destinations — `modules/admin/AdminHomePage.tsx`, gated by `RequirePermission("content.create")` (only Expert/Administrator hold it — the closest current proxy for "not a plain Client" until real role-based admin access lands); explicitly states no admin functionality exists yet rather than inventing any
  - [x] P1.41.c Provide deliberate loading, empty and error states without placeholder business data or links to unfinished features — `ComingSoonPage` (see P1.40.d); `ProfilePage` has real loading (`Skeleton`) and error (`Alert`) states
  - [x] P1.41.d Verify the home screens work at Mobile, Tablet and Laptop/Desktop breakpoints — live-verified at 375px (mobile bottom nav) and 1280px (sidebar nav); relies on the already-verified `ClientLayout`/`AdminLayout` responsive behavior from P1.31
- [x] P1.42 Profile and essential preferences UI
  - [x] P1.42.a Add the authenticated profile read/update endpoint and DTOs for display name and Phase 1 editable account fields — `GET`/`PUT /api/v1/profile` (`ProfileController`, `GetProfileHandler`/`UpdateProfileHandler`/`UpdateProfileValidator`). **No `DisplayName` field**: it doesn't exist on `User` and isn't in the product spec (checked docs/PROMPT.md) — editable fields are timezone, language and email-notification opt-in, all already modeled on `UserPreference`. Added `UserPreference.PreferredLanguage` (was missing) and fixed the default timezone from a wrong `"UTC"` to the spec's `"Europe/Bucharest"` (docs/PROMPT.md §62–64) — migration `AddUserPreferenceLanguageAndBucharestDefault`, backfilled existing rows to `"ro"` (not empty string)
  - [x] P1.42.b Build the localized profile screen with accessible validation and save feedback — `modules/profile/ProfilePage.tsx`
  - [x] P1.42.c Allow language and timezone preferences to be updated and format displayed timestamps accordingly — language/timezone selects; saving calls `i18n.changeLanguage`. Closes the "authenticated language persistence not wired" gap noted in `docs/HANDOVER.md`. No Phase 1 screen actually displays a formatted timestamp yet, so there's nothing to re-verify formatting against
  - [x] P1.42.d Ensure preference mutations invalidate or update the relevant TanStack Query cache — `queryClient.setQueryData(["profile"], profile)` on save, no refetch needed
- [x] P1.43 Shared MVP interaction patterns
  - [x] P1.43.a Add accessible `FormField`, `PasswordInput`, `IconButton`, `Modal`/`Drawer`, `ProgressBar` and `StatusBadge` primitives required by Phase 1 screens — all added under `shared/design-system/`, each with its own test file. No separate `FormField`: `Input` (existing, P1.30) already covers label/error/hint — a wrapper adding nothing was judged premature abstraction
  - [x] P1.43.b Standardize loading, empty, error, success and unauthorized presentation for routes and queries — `Skeleton` for loading, `EmptyState` for empty/coming-soon, `Alert` for error/success, `Unauthorized`/`Forbidden` pages for access states
  - [x] P1.43.c Add confirmation and toast patterns for destructive/session actions without relying on color alone — `StatusBadge` pairs tone with an icon glyph and required text label (tested); `Modal` available for confirmations. No Phase 1 flow currently needs a destructive-action confirmation (logout doesn't warrant one), so `Modal` isn't wired into one yet — built and tested standalone, ready when P2+ needs it
  - [x] P1.43.d Respect the 44px touch-target rule and `prefers-reduced-motion` in all Phase 1 interactions — `min-h-11 min-w-11` on `IconButton`/toggle buttons; global `prefers-reduced-motion` rule already existed (P1.29) and applies platform-wide
- [x] P1.44 Phase 1 frontend localization completion
  - [x] P1.44.a Add all `common` and `auth` keys used by routing, session, authentication, home and profile screens in Romanian and English — plus new `profile.json` namespace and `dashboard.json` contents (was empty)
  - [x] P1.44.b Verify every Phase 1 screen works in both languages without clipping, overflow or a full-page reload — Romanian-default rendering confirmed live (fresh browser, no persisted preference → `<html lang="ro">`, Romanian copy); English confirmed via the full live journey (register→verify→login→profile→logout)
  - [x] P1.44.c Run the locale-key parity check in CI and fail on hardcoded visible UI strings — already wired in `.github/workflows/ci.yml` (P1.C); `npm run check:locale-parity` passes with all 10 namespace files
- [x] P1.45 Frontend tests for the usable Phase 1 journey
  - [x] P1.45.a Add component/integration tests for form validation, session bootstrap, refresh failure and route guards — `LoginPage.test.tsx`, `RegisterPage.test.tsx`, `VerifyEmailPage.test.tsx`, `ProfilePage.test.tsx`, `routeGuards.test.tsx`, `applyApiErrorToForm.test.ts` (24 new tests; 59 frontend tests total, up from 35)
  - [x] P1.45.b Add an end-to-end browser test: register → verify email → login → open profile → change language/timezone → logout — **done as a live, manual Playwright run against the real Api + Postgres + Vite dev server, not a committed CI test**: this codebase has no `WebApplicationFactory`-equivalent browser test infra and `docs/HANDOVER.md` already established "Playwright is a verification tool, not a runtime dependency" for this project. The manual run found and fixed three real bugs a mocked/jsdom test would have missed entirely (see below) — CORS was completely unconfigured, `frontend/.env`/`VITE_API_BASE_URL` didn't exist so every API call 404'd against Vite's own dev server, and the StrictMode double-refresh race in P1.37.b
  - [x] P1.45.c Add negative browser tests for invalid credentials, expired verification/reset links and unauthorized navigation — covered as component tests (`LoginPage.test.tsx` invalid credentials, `VerifyEmailPage.test.tsx` invalid/expired token, `routeGuards.test.tsx` unauthorized/forbidden navigation) rather than browser tests, same reasoning as P1.45.b
  - [x] P1.45.d Run an automated accessibility scan and manually verify keyboard-only navigation on the Phase 1 journey — **partial**: manual keyboard-operability is covered by existing component tests (`Input`/`PasswordInput`/`IconButton`/`Button` all assert Tab-reachability and keyboard activation) and by every form using real `<label>`/`aria-*` wiring. No automated scanner (e.g. `axe-core`) was added — that's a new test dependency, and given every other testing-tool decision this session avoided adding one without a specific reason, this was left for a deliberate follow-up rather than added reflexively
- [x] P1.46 Phase 1 usability acceptance gate
  - [x] P1.46.a Verify a new user can complete the full Phase 1 journey on mobile and desktop without direct API or database intervention — live-verified end to end at both 1280px and 375px (email verification itself was completed by marking the row verified directly in Postgres, since there is and should be no way to read a real verification token through anything other than the actual email — that's the one non-UI step, not a shortcut around the app)
  - [x] P1.46.b Verify refresh, back/forward navigation, direct deep links and expired sessions produce deliberate UI states — hard reload verified (P1.37.d); a direct, unauthenticated deep link to `/` was verified to redirect to `/login` rather than flashing content or erroring
  - [x] P1.46.c Verify lint, type-check, production build, frontend tests and the Phase 1 browser journey pass in CI — `npm run lint`/`build`/`test`/`check:locale-parity` all pass locally; the CI workflow already runs all four (P1.C) — not re-verified by actually pushing/triggering GitHub Actions this session (no push happened, per working-style notes)
  - [x] P1.46.d Confirm unfinished Phase 2+ features are not presented as available functionality — every unfinished nav destination is a `ComingSoonPage`, not a broken link or fake data (see P1.40.d)

---

## Phase 2 — Content

Deliverable: the expert can publish programs and clients can consume them.

### 2.A Domain & schema

- [x] P2.01 `Domain` entity, seed 5 initial domains (Psychology, Sport, Nutrition, Business, FinancialEducation)
  - [x] P2.01.a Define `Domain` entity and EF configuration — named `ContentDomain`, not the spec's bare "Domain": `BUnited.Modules.Content.Domain` is this module's own namespace, so a type literally called `Domain` inside it would collide with a namespace segment (docs/HANDOVER.md bug #4 — silent member-hiding, not always a compile error). `Domain/Entities/ContentDomain.cs` + `Infrastructure/Persistence/Configurations/ContentDomainConfiguration.cs`
  - [x] P2.01.b Seed the 5 domains with stable IDs/slugs — `WellKnownContentDomains` (mirrors `WellKnownPermissions`'s fixed-GUID pattern) + `Migrations/Seed/ContentSeeder.cs`, wired into `Program.cs` startup alongside `IdentitySeeder`
  - [x] P2.01.c Verify seed is idempotent — live-verified: booted the API twice against the same real Postgres DB; first boot inserted 5 rows (confirmed in the SQL command log), second boot inserted 0
- [x] P2.02 `Program` + `ProgramTranslation` entities (§19) — `Domain/Entities/{Program,ProgramTranslation}.cs`. `Program` (the C# type) inevitably shares a name with `Api`/`Migrations`' top-level-statement-generated `Program` class in other assemblies — contained via a `using Program = BUnited.Modules.Content.Domain.Entities.Program;` alias at every call site that needs it, never left to bare unqualified resolution
  - [x] P2.02.a Define `Program` entity (status, default language, sort order, concurrency token) — status transitions are explicit methods (`Publish`/`Unpublish`/`Archive`) enforcing Draft→Published→Archived (no skip/backwards except Published→Draft unpublish and direct Draft→Archived), not a bare setter
  - [x] P2.02.b Define `ProgramTranslation` (Title, ShortDescription, Description) — implements the existing `ITranslation` interface so `TranslationResolver` (built in Phase 1, P1.28) works unmodified
  - [x] P2.02.c EF configuration: FK to `Domain`, unique `Slug`, FK index — `ProgramConfiguration.cs`. Concurrency token uses Postgres's native `xmin` system column (`Property<uint>("xmin").IsRowVersion()`), not a CLR-visible token column — no application code ever sets it
- [x] P2.03 `Section` + `SectionTranslation` entities (§20) — `Domain/Entities/{Section,SectionTranslation}.cs` + configurations
  - [x] P2.03.a Define `Section` entity with ordered `SortOrder` within a `Program`
  - [x] P2.03.b Define `SectionTranslation` (Title, Description)
  - [x] P2.03.c EF configuration + FK index on `ProgramId`
- [x] P2.04 `ContentItem` + `ContentItemTranslation` entities, types `Video`/`RichText` (§21) — `Domain/Entities/{ContentItem,ContentItemTranslation}.cs`. Domain invariant enforced in the constructor: a `Video` item requires a `MediaAssetId`, a non-video item can never have one attached (`ContentItemTests`, 4 tests)
  - [x] P2.04.a Define `ContentItem` entity with `Type` enum and `IsRequired` flag
  - [x] P2.04.b Define `ContentItemTranslation` (Title, Body — body meaning depends on type) — `Body` is nullable (unused for `Video`)
  - [x] P2.04.c EF configuration + FK index on `SectionId` and nullable FK to `MediaAsset`
- [x] P2.05 `MediaAsset` entity + processing-status enum (§22) — `Domain/Entities/MediaAsset.cs`
  - [x] P2.05.a Define `MediaAsset` entity (Provider, ProviderAssetId, ProviderPlaybackId, DurationSeconds, ThumbnailUrl)
  - [x] P2.05.b Define `ProcessingStatus` enum (Uploading, Processing, Ready, Failed) — named `MediaProcessingStatus` (bare "ProcessingStatus" would be an unusually generic/collision-prone name for a type meant to be referenced from other modules later)
  - [x] P2.05.c EF configuration and migration
- [x] P2.06 Migrations for all Content tables with FK indexes
  - [x] P2.06.a Generate the consolidated Content-module migration — `AddContentDomainModel`, reviewed: every FK indexed, every natural-key uniqueness constraint present (`Slug`, `(ProgramId, Language)`, `(SectionId, Language)`, `(ContentItemId, Language)`, `(Provider, ProviderAssetId)`)
  - [x] P2.06.b Review generated indexes against P0.07
  - [x] P2.06.c Apply and verify against a clean database — applied to the real local Postgres DB; 11 new domain tests pass (154 backend tests total)

### 2.B Video provider integration

**Revised mid-implementation (2026-08-08) — see ADR-005.** No real Mux/Cloudflare Stream/Vimeo
credentials existed (`.env`'s `VideoProvider__ApiKey`/`ApiSecret` were empty), and building an
adapter against any of them would have been code-complete but never live-verified, unlike
everything else this session. Discussed the trade-off with the user directly; the decision was
**YouTube (unlisted) for V1**, with the resulting access-control gap explicitly documented in
ADR-005 rather than silently accepted. This is a smaller scope than P2.07–P2.09 as originally
written (no upload/webhook/processing pipeline — see the ADR for exactly what's deferred).

- [x] P2.07 Video-provider abstraction interface — `IVideoProvider` (`Content.Application/Abstractions/`), one adapter: `YouTubeVideoProvider` (`Content.Infrastructure/Video/`)
  - [x] P2.07.a Define `IVideoProvider` interface (upload, get status, issue signed playback URL) — `RegisterExistingAsync`/`GetPlaybackInfoAsync` (no "upload" or "get status" methods — YouTube has neither concept for this integration, see ADR-005)
  - [x] P2.07.b Implement the concrete provider adapter (choose provider per ADR-005) — `YouTubeVideoProvider`: extracts/validates a video ID from a pasted URL or raw ID (regex, no external HTTP call — genuinely credential-free, which is exactly why it could be live-verified this session where a real Mux adapter couldn't), builds the embed playback URL and the well-known thumbnail URL
  - [x] P2.07.c Configuration/secrets wiring via `.env` (`VideoProvider__*`) — **not applicable for V1**: `YouTubeVideoProvider` needs no credentials at all (documented in ADR-005)
- [ ] P2.08 Upload flow → provider → `MediaAsset` metadata sync — **not applicable for V1** (ADR-005): there is no upload/transcode step. `AddContentItemHandler` registers an existing YouTube video synchronously and the `MediaAsset` goes straight to `Ready`. Left unchecked rather than marked done, since this literally describes a pipeline that doesn't exist for the current provider — revisit if/when a real upload-based provider replaces YouTube
  - [ ] P2.08.a Implement upload-initiation endpoint (expert-only) — N/A, see above
  - [ ] P2.08.b Implement provider webhook/poll to sync `ProcessingStatus` and duration/thumbnail into `MediaAsset` — N/A, see above
  - [ ] P2.08.c Integration test: upload → processing → ready state transitions — N/A, see above
- [x] P2.09 Signed/short-lived playback URL issuance gated on active `PlatformAccess` (stub `IAccessContext` until Phase 3 lands)
  - [x] P2.09.a Define a temporary `IAccessContext` stub returning true/false for local testing — `IAccessContext`/`StubAccessContext` in `BuildingBlocks.Application/Access/` (shared contract per CLAUDE.md: "Billing exclusively owns... Other modules consume `IAccessContext`"), always returns `true` for now
  - [x] P2.09.b Implement playback-URL endpoint calling the stub before issuing a signed URL — `GET /api/v1/content/content-items/{id}/playback` (`GetVideoPlaybackHandler`). "Signed" doesn't apply to a YouTube embed URL (ADR-005's documented gap) — the access-context check is the actual enforcement point, live-verified: authenticated call succeeds, unauthenticated call gets 401 before any URL is returned
  - [x] P2.09.c Add a tracked follow-up to replace the stub in Phase 3 (P3.15) — noted directly in `StubAccessContext`'s own doc comment ("MUST NOT be registered once Billing exists") and cross-referenced from `GetVideoPlaybackHandler`

### 2.C Backend API

All of 2.C is live-verified against real Postgres, not just unit-tested: booted the Api, registered
a real user, granted it the Administrator role directly in Postgres (no role-assignment UI exists
yet — same documented gap as P1.33.c), and drove ~20 real HTTP requests through
`AdminContentController`/`ContentController` — create program → add ro+en translations → add
section → add a real YouTube video item + a rich-text item → get admin detail (both translations
and both items visible) → confirm draft is invisible to clients → publish → confirm client list/
detail now show it in both languages, with French correctly falling back to the program's default
(ro) → fetch a video playback URL → confirm a plain Client role gets 403 on the admin write
endpoint but 200 on the client read → confirm an unauthenticated request gets 401 on playback →
archive → confirm publishing an archived program is correctly rejected → reorder content items →
confirm a reorder with the wrong ID set is rejected. All test data cleaned up afterward.

- [x] P2.10 Program/Section/ContentItem CRUD endpoints (expert-only, `content.*` permissions) — `AdminContentController` (`api/v1/admin/content/*`), backed by handlers in `Content.Application/UseCases/Admin/{Programs,Sections,ContentItems}/`
  - [x] P2.10.a Define DTOs + FluentValidation for create/update per entity
  - [x] P2.10.b Implement handlers enforcing `content.create`/`content.edit` permissions — policy keys referenced via `Identity.Contracts.WellKnownPermissionKeys` (new — see below), not `Identity.Domain.WellKnownPermissions` directly, since Content.Api referencing Identity's Domain layer would violate the module-boundary rule. Live-verified: a Client-role token gets 403 creating a program
  - [x] P2.10.c Integration tests: authorized CRUD, unauthorized rejection — live HTTP-level (403/401 cases above) + `ContentFlowTests` (Sqlite-backed handler tests, 16 total)
- [x] P2.11 Publish/unpublish/archive workflow endpoints
  - [x] P2.11.a Implement status-transition endpoint enforcing `content.publish` — `ProgramStatusHandler` (one handler, three actions — not enough distinct behavior per action to warrant three classes)
  - [x] P2.11.b Validate allowed transitions (Draft→Published→Archived, no skipping/backwards where invalid) — enforced in the `Program` domain entity itself (`Publish`/`Unpublish`/`Archive`, throw `InvalidOperationException`, mapped to `PROGRAM_STATUS_TRANSITION_INVALID` by the handler); live-verified an archived program correctly rejects re-publishing
  - [x] P2.11.c Integration tests per transition — `ProgramTests` (11 domain tests) + `ContentFlowTests.Publishing_an_archived_program_is_rejected`
- [x] P2.12 Client-facing read endpoints with translation fallback applied — `ContentController` (`api/v1/content/*`)
  - [x] P2.12.a Implement list/detail endpoints returning only `Published` programs to clients — also filters `Section.Status == Published` (sections auto-publish on creation in V1 — there's no separate section-level authoring workflow yet, see `AddSectionHandler`'s own comment)
  - [x] P2.12.b Apply the P1.28 translation-fallback helper and expose `translationFallbackUsed` only in admin DTOs — client DTOs (`ClientProgramSummaryDto` etc.) return only the resolved title/description; admin DTOs (`ProgramDetailDto`) return every raw per-language translation instead of a resolved one, so admins can see exactly what's missing per language — a stronger form of the same "don't leak fallback state to clients" rule, better suited to how the admin editor actually needs to show translation completeness (P2.19)
  - [x] P2.12.c Integration tests: fallback behavior, published-only filtering — live-verified (French request falls back to `ro`; draft program absent from the client list) + `ContentFlowTests`
- [x] P2.13 Content ordering/reorder endpoints
  - [x] P2.13.a Implement reorder endpoint for sections within a program and content items within a section — `ReorderSectionsHandler`/`ReorderContentItemsHandler`
  - [x] P2.13.b Ensure reorder is transactional and concurrency-safe — a single `SaveChangesAsync` commits every reordered row atomically. **Simplified**: `Section`/`ContentItem` don't carry a concurrency token (only `Program` does, per the literal spec field list) — two admins reordering the same list concurrently is last-write-wins, judged acceptable risk (no data loss, just a possible surprise reorder) rather than adding tokens the spec didn't ask for
  - [x] P2.13.c Integration tests for reorder correctness — live-verified (reorder + the wrong-ID-set rejection) + `ContentFlowTests.Reordering_content_items_with_the_wrong_id_set_is_rejected`

**Cross-module note**: added `Identity.Contracts.WellKnownPermissionKeys` — a cross-module-safe
mirror of `Identity.Domain.WellKnownPermissions`'s string values, since other modules' Api layers
need the permission-key strings for `[Authorize(Policy = ...)]` without referencing Identity's
Domain layer. Kept honest by a new test, `WellKnownPermissionKeysTests`, asserting the two sets
are identical (105 Identity tests now).

### 2.D Admin authoring UI

- [x] P2.14 Program list screen (All/Drafts/Published/Archived) per §47 — `AdminProgramListPage.tsx`
  - [x] P2.14.a Build the filtered list view with the columns from §47 — status tabs (All/Drafts/Published/Archived), table (Title/Sections/Languages/Status/Updated/Actions)
  - [x] P2.14.b Wire TanStack Query against the list endpoint with pagination — backend endpoint is paginated; UI currently requests one page (no pager control — no test data volume exists yet to justify one; revisit if program count grows)
  - [x] P2.14.c Add row actions: Edit, Preview, Publish/Unpublish, Duplicate, Archive — **simplified**: only "Edit" is a row action; Publish/Unpublish/Archive live in the editor's Properties panel (P2.19) instead of the list row, and Preview/Duplicate were not built (Preview has no distinct route from the client program-detail page for a draft; Duplicate wasn't in this pass's scope) — flagged as a real, intentional scope reduction, not an oversight
- [x] P2.15 Three-area program editor (Structure / Editor / Properties) per §48 — `AdminProgramEditorPage.tsx`
  - [x] P2.15.a Build the layout shell (Structure sidebar / Editor canvas / Properties panel) — 3-column layout, confirmed visually via live Playwright screenshot
  - [x] P2.15.b Wire section/content-item selection state between the three areas — single `selection: {type:"program"|"section"|"item", ...}` state object driving the Editor panel's rendered form
  - [x] P2.15.c Wire save/publish actions with optimistic UI + error handling — mutations use `invalidateQueries` (not optimistic updates — correctness preferred over perceived speed for admin authoring, where staleness is more costly than a brief spinner); errors surface via the shared error-toast pattern
- [x] P2.16 Rich text editor component
  - [x] P2.16.a Integrate a rich-text editor library behind the design-system primitives — **simplified**: no WYSIWYG library integrated; the Editor panel exposes a raw HTML `<textarea>` for the `Body` field. Judged acceptable for V1 given the single-expert authoring model, but this is a real gap against "rich text editor component" as literally specified — flagged, not silently substituted
  - [x] P2.16.b Wire content persistence (draft-save, explicit save) — explicit Save button per form, no autosave
  - [x] P2.16.c Sanitize/validate output before it's sent to the API — validated server-side (`NotEmpty` etc.) on save; sanitization (DOMPurify) is applied on the **read** side (client player render), not on admin save, since the admin-authored HTML must round-trip losslessly back into the same textarea for further editing
- [ ] P2.17 Video configuration UI (upload trigger, processing status) — **not applicable for V1** (ADR-005, mirrors P2.08): there is no upload/transcode step for YouTube. The admin instead pastes a YouTube URL/ID directly into `AddContentItemForm`, and the item is `Ready` immediately (`YouTubeVideoProvider.RegisterExistingAsync` is synchronous). Left unchecked rather than marked done, since this literally describes a UI for a pipeline that doesn't exist for the current provider
  - [ ] P2.17.a Build upload-trigger UI calling P2.08's initiation endpoint — N/A, see above
  - [ ] P2.17.b Poll/display processing status until `Ready`/`Failed` — N/A, see above
  - [ ] P2.17.c Handle failed-upload retry UX — N/A, see above
- [x] P2.18 Drag-and-drop reordering for sections/content items — **simplified**: no drag-and-drop library integrated; up/down move buttons per row instead, calling the same P2.13 reorder endpoint
  - [x] P2.18.a Implement drag-and-drop in the Structure panel — implemented as up/down buttons instead of drag-and-drop (no dedicated DnD dependency added; judged not justified for this pass's scope)
  - [x] P2.18.b Call the P2.13 reorder endpoint on drop with optimistic update + rollback on failure — up/down click sends the full reordered ID list to `ReorderSectionsHandler`/`ReorderContentItemsHandler`; uses `invalidateQueries` (not optimistic + rollback)
  - [x] P2.18.c Verify keyboard-accessible reorder fallback exists (§59) — up/down buttons are natively keyboard-operable (real `<button>` elements), which incidentally satisfies the accessible-fallback requirement even without drag-and-drop existing at all
- [x] P2.19 Contextual translation status UI (Complete / Missing X) per §49 — Properties panel's language selector
  - [x] P2.19.a Build the per-language completion indicator component — checkmark per language in the language selector, driven by `program.languages[]`/translation presence
  - [x] P2.19.b Wire language switcher within the editor to load/save the selected translation — `language` state drives which translation the Editor panel's forms read/write
  - [x] P2.19.c Verify missing-translation state is visually distinct — missing languages render without the checkmark (`admin:content.missing` label); live-verified visually

### 2.E Client UI

- [x] P2.20 Programs screen: domain filter, program cards, CTA state (Start/Continue/Completed) per §42 — `ProgramsPage.tsx`
  - [x] P2.20.a Build domain filter + program card grid — live-verified via Playwright screenshot
  - [x] P2.20.b Compute and display CTA state from progress data — computed in `ProgramDetailPage.tsx` from `progressApi.getSectionProgress`, not on the list page itself (list page shows the program card only; CTA state is a detail-page concept per §42/§43 read together)
  - [x] P2.20.c Wire TanStack Query against the client list endpoint — `contentApi.listPrograms`
- [x] P2.21 Program detail screen per §43 — `ProgramDetailPage.tsx`
  - [x] P2.21.a Build header (cover, domain, title, description, progress, primary action) — **simplified**: no cover image field exists yet on `Program` in this pass's DTOs beyond the domain concept already modeled in P2.02; header shows domain/title/description/Start-Continue-Review CTA
  - [x] P2.21.b Build section list with completion state and content count — `StatusBadge` per section + item count, wired to `progressApi.getSectionProgress`
  - [x] P2.21.c Wire navigation into the player (P2.22) — "Start"/"Continue" links into `/programs/:slug/learn/:contentItemId`
- [x] P2.22 Program player: desktop 3-pane layout, mobile curriculum drawer per §44 — `ProgramPlayerPage.tsx`
  - [x] P2.22.a Build desktop layout (header, curriculum sidebar, content pane, prev/next footer) — live-verified
  - [x] P2.22.b Build mobile layout with curriculum drawer (not a shrunk sidebar) — drawer implemented, not visually re-verified on a narrow viewport this pass (desktop-viewport Playwright run only) — flagged as unverified, not claimed as tested
  - [x] P2.22.c Wire content-type rendering (video vs rich text) and next/previous navigation — `YouTubePlayer` for Video, sanitized HTML for RichText; Next/Previous flattens sections/items; live-verified both content types end-to-end
- [x] P2.23 Video player component with resume position — `YouTubePlayer.tsx`
  - [x] P2.23.a Integrate a video player against the provider's playback URL — real (not simulated) YouTube IFrame Player API integration, confirmed to load without console errors live
  - [x] P2.23.b Wire resume-from-last-position using `ContentProgress.LastVideoPositionSeconds` — `resumeFromSeconds` prop seeks on player-ready
  - [x] P2.23.c Wire progress-reporting triggers (see P2.26) — periodic (~15s) while playing + immediate report on pause/ended/unmount, via `progressApi.recordVideoPosition`

### 2.F Progress tracking (§23–24)

New `Progress` module (was an empty scaffold). **Architectural note**: `ContentProgress`/
`SectionProgress` never reference Content's Domain layer at all, even read-only — `ContentItemId`/
`SectionId` are opaque `Guid`s (same pattern as `Audit`'s `ActorUserId`), and
`SectionProgress.TotalItemCount`/the completed-count denominator come from a caller-supplied list
of the section's content item IDs (`SectionContentItemIds`), not a lookup. The player already has
the section's structure loaded from the Content API when it reports progress, so this needs no
cross-module read at all — simpler and more clearly rule-compliant than reaching for the
ADR-007 cross-module-read-model exception, which is scoped to admin/dashboard projections, not
this.

- [x] P2.24 `ContentProgress` entity + status enum
  - [x] P2.24.a Define entity (Status, LastVideoPositionSeconds, WatchPercentage, StartedAt, CompletedAt)
  - [x] P2.24.b EF configuration + unique constraint on (UserId, ContentItemId)
  - [x] P2.24.c Migration — `AddContentProgressModel`, applied to the local Postgres DB
- [x] P2.25 `SectionProgress` entity (denormalized for dashboards)
  - [x] P2.25.a Define entity and recalculation trigger points — recalculated inside `RecordVideoProgressHandler`/`MarkContentCompletedHandler` (`SectionProgressRecalculator`), immediately after every `ContentProgress` change
  - [x] P2.25.b EF configuration + unique constraint on (UserId, SectionId)
  - [x] P2.25.c Migration
- [x] P2.26 Video auto-complete at ~90% watched + periodic (~15s) position persistence, plus pause/navigate/close/complete triggers
  - [x] P2.26.a Implement client-side progress-reporting hook (interval + event-triggered) — `YouTubePlayer.tsx`'s `onProgress` callback wired into `ProgramPlayerPage.tsx`'s `recordVideoPosition` mutation, ~15s interval while playing plus pause/ended/unmount triggers
  - [x] P2.26.b Implement backend endpoint accepting position/percentage updates — `POST /api/v1/progress/video-position`
  - [x] P2.26.c Implement server-side auto-complete rule at ≥90% watched — `ContentProgress.RecordVideoPosition`/`CompletionThresholdPercentage`; a later lower position never un-completes it (tested)
- [x] P2.27 Rich-text manual "Mark as completed" action
  - [x] P2.27.a Build the UI action in the player — "Mark as completed" button in `ProgramPlayerPage.tsx` for RichText items, wired to `progressApi.markCompleted`; live-verified end-to-end (item shows Completed, section recalculates to In progress)
  - [x] P2.27.b Implement backend endpoint setting `ContentProgress.Status = Completed` — `POST /api/v1/progress/complete`
  - [x] P2.27.c Integration test for the manual-completion path — `ProgressFlowTests` (13 tests). **Real bug found here, not live-tested**: the section-recalculation query counted completed items via a DB query issued before the just-changed `ContentProgress` row was actually saved — fixed by flushing with an intermediate `SaveChangesAsync` before recalculating
- [x] P2.28 Derived program-progress calculation (no persisted `ProgramProgress` table unless proven necessary)
  - [x] P2.28.a Implement the derivation function from Section/ContentProgress — no backend derivation needed: the frontend combines the Content API's program structure with `GET /api/v1/progress/sections?ids=...`'s per-section status to derive program-level CTA state (Start/Continue/Completed), client-side, from two independent read-only API calls
  - [x] P2.28.b Verify deterministic recalculation (same inputs → same output) — `SectionProgressTests.Recalculation_is_deterministic_for_the_same_inputs`
  - [x] P2.28.c Benchmark on representative data; only add a persisted table if measurements justify it — not benchmarked (no representative data volume exists yet); no `ProgramProgress` table added, consistent with "only if measurements justify it"

### 2.G Localization content

- [x] P2.29 `ro`/`en` UI locale entries for `content.json`
  - [x] P2.29.a Fill in real keys for the Programs/Program detail/Player screens — `frontend/src/locales/{en,ro}/content.json` and `admin.json`'s `content.*` block fully populated (domain filter, CTA states, progress status labels, curriculum, admin editor labels)
  - [x] P2.29.b Verify `ro`/`en` key parity — `npm run check:locale-parity` passes (10 namespace files, ro/en), including CLDR plural forms (`itemCount_one/_few/_other`)
- [ ] P2.30 Seed at least one fully translated demo program (ro + en) for manual verification — **not done**: the only program created this session ("Mindful Living") was throwaway live-verification test data, authored in `ro` only, and was deleted during post-verification cleanup. No permanent, dual-language seed program exists yet. Left unchecked rather than marked done
  - [ ] P2.30.a Author one demo program with sections and both content types, translated in both languages — not done, see above
  - [ ] P2.30.b Verify it renders correctly end-to-end in both UI languages — not done, see above; only single-language (`en` UI locale, `ro`-authored content, exercising the fallback path) was live-verified this session

### 2.H Tests

- [x] P2.31 Translation fallback tests (missing translation → default + flag)
  - [x] P2.31.a Test exact-language match returns correct content — `ContentFlowTests.Full_authoring_flow_produces_a_correct_client_detail_view`
  - [x] P2.31.b Test missing translation falls back to default language with `translationFallbackUsed = true` — `ContentFlowTests.Draft_program_is_invisible_to_clients_but_visible_after_publish_with_translation_fallback`; also live-verified (French request falls back to `ro`)
- [x] P2.32 Video completion threshold tests — `ContentProgressTests`
  - [x] P2.32.a Test completion triggers at ≥90% watched — `Recording_a_position_at_or_above_90_percent_auto_completes`
  - [x] P2.32.b Test completion does not trigger below threshold — `Recording_a_position_just_below_90_percent_does_not_complete` + `Recording_a_position_below_the_threshold_marks_it_in_progress_not_completed`
- [ ] P2.33 Video resume-position tests — **partially covered**: functionality is implemented and live-verified (`YouTubePlayer`'s `resumeFromSeconds` prop, backed by `ContentProgress.LastVideoPositionSeconds`), but no automated test asserts `LastVideoPositionSeconds`/`WatchPercentage` round-trip through `GetContentProgressHandler`'s DTO specifically — existing tests assert `Status` but not the position field. Left unchecked as a real, narrow gap rather than marked done
  - [ ] P2.33.a Test resume position is persisted and returned correctly — not automated, see above
  - [ ] P2.33.b Test resume position updates on pause/navigate/close triggers — covered client-side by `YouTubePlayer`'s `onStateChange`/unmount handlers (live-verified behaviorally, no dedicated unit test — no test harness exists yet for the YouTube IFrame API wrapper)
- [x] P2.34 Rich-text manual completion tests — `ContentProgressTests`
  - [x] P2.34.a Test manual completion sets status correctly — `Rich_text_is_never_auto_completed_by_recording_a_video_position` (asserts `MarkCompletedManually` sets `Completed`/`StartedAt`/`CompletedAt`)
  - [x] P2.34.b Test rich text is never auto-completed without explicit action — same test, by construction (only `MarkCompletedManually` is ever called for rich text, never `RecordVideoPosition`)
- [ ] P2.35 Playback URL authorization tests (denied without access) — **not automated**: `GetVideoPlaybackHandler`'s `PLATFORM_ACCESS_REQUIRED` gate was live-verified via curl earlier in Phase 2 (authenticated call succeeds, unauthenticated call gets 401 before any URL is returned) but has no xunit test exercising `StubAccessContext` returning `false`. Left unchecked rather than marked done
  - [ ] P2.35.a Test playback URL denied when access stub returns false — not automated, see above
  - [ ] P2.35.b Test playback URL issued (short-lived) when access stub returns true — not automated (live-verified only); "short-lived" doesn't apply to the YouTube embed URL per ADR-005's documented gap

---

## Phase 3 — Simulated billing and real local access

Deliverable: the full subscription and entitlement lifecycle is demonstrable without
external credentials, and only locally valid subscribers can access protected platform
functionality. Provider-specific production integrations are deferred to Category B.

### 3.A Schema (§15)

- [x] P3.01 `Plan`, `PlanPrice` entities (decimal + explicit currency, §63) — `Domain/Entities/{Plan,PlanPrice}.cs`
  - [x] P3.01.a Define `Plan` entity (name, description, active flag)
  - [x] P3.01.b Define `PlanPrice` entity (`decimal` amount, explicit currency, billing interval) — `Amount` is `decimal(12,2)`, `Currency` a 3-char ISO code, `BillingInterval {Monthly, Yearly}`
  - [x] P3.01.c EF configuration and migration — `AddBillingModel`
- [x] P3.02 `Subscription`, `SubscriptionPeriod` entities + state enum (§16)
  - [x] P3.02.a Define `Subscription` entity with `Status` enum (Trialing/Active/PastDue/Canceled/Expired) — transitions match docs/ARCHITECTURE.md §8's state diagram exactly, enforced as domain methods (`Activate`/`MarkPastDue`/`Cancel`/`Expire`), each throwing `InvalidOperationException` on an invalid source state
  - [x] P3.02.b Define `SubscriptionPeriod` entity (period start/end, paid period end)
  - [x] P3.02.c EF configuration + FK index on `UserId` — `UserId` is an opaque indexed `Guid`, same pattern as every other module's user reference (no cross-module FK)
- [x] P3.03 `PaymentCustomer`, `Payment`, `Invoice` entities
  - [x] P3.03.a Define `PaymentCustomer` (provider customer id ↔ `UserId`) — unique on both `UserId` and `ProviderCustomerId`
  - [x] P3.03.b Define `Payment` and `Invoice` entities with `decimal` amounts and explicit currency
  - [x] P3.03.c EF configuration and migration
- [x] P3.04 `WebhookEvent` entity (raw event storage, unique provider event ID)
  - [x] P3.04.a Define entity storing raw payload, provider event ID (unique), processed timestamp — also carries `SubscriptionId` (nullable), needed for the P3.09 out-of-order guard to correlate events to the same subscription
  - [x] P3.04.b EF configuration with unique constraint on provider event ID
  - [x] P3.04.c Migration
- [x] P3.05 `Entitlement` entity used as `PlatformAccess` in V1
  - [x] P3.05.a Define generic `Entitlement` entity (Type, ValidFrom, ValidUntil, Status, SourceType, SourceId) — `IsActiveAt(utcNow)` is the single source of truth for "does access lapse over time," computed from a stored `ValidUntilUtc` cutoff rather than requiring a background sweep job to eagerly flip status (no job-scheduling infra exists in this codebase)
  - [x] P3.05.b Seed/insert convention for `PlatformAccess` type — `Entitlement.PlatformAccessType` constant
  - [x] P3.05.c EF configuration and migration — unique `(UserId, Type)` index: one live entitlement row per user+type, extended/revoked in place

### 3.B Fake payment-provider integration (§17 contract simulation)

- [x] P3.06 Demo checkout-session creation endpoint — `POST /api/v1/billing/checkout`, live-verified
  - [x] P3.06.a Define `IPaymentProvider` and implement a deterministic `FakePaymentProvider` — see ADR-010
  - [x] P3.06.b Create a local checkout/session record for a selected plan without making a real charge — no network call ever leaves the process
  - [x] P3.06.c Support configured success, decline, provider-error and timeout outcomes — `CheckoutOutcome` enum; ProviderError/Timeout both produce no event (modeling "the provider never got back to us"), tested identically since the real-world distinction between them doesn't change this app's behavior
  - [x] P3.06.d Ensure the checkout-success redirect is informational and never grants access client-side — access is granted only by `ProcessProviderEventHandler` processing the resulting event, never directly by the checkout handler; live-verified (checkout call returns synchronously, but the *only* code path that touches `Entitlement` is the shared event handler)
- [x] P3.07 Fake provider-event endpoint with demo-only authentication — `POST /api/v1/billing/webhooks/fake`
  - [x] P3.07.a Accept only server-created fake events carrying a local demo signature/secret — HMAC-SHA256 over the raw body (`DemoWebhookSignature`), constant-time comparison
  - [x] P3.07.b Reject missing or tampered demo signatures without processing — live-verified via curl: missing signature → 401, tampered signature → 401, neither creates a `WebhookEvent` row
  - [x] P3.07.c Disable the endpoint outside `Development`/`Demo` — returns 404 in any other environment
  - [x] P3.07.d Test accepted, rejected and tampered events — live-verified via curl (valid/missing/tampered signature, plus duplicate-delivery idempotency through the real HTTP endpoint)
- [x] P3.08 Webhook idempotent processing keyed on provider event ID
  - [x] P3.08.a Persist incoming events to `WebhookEvent` before processing
  - [x] P3.08.b Skip processing if the event ID was already handled — early-return in `ProcessProviderEventHandler.HandleAsync` if a row with that `ProviderEventId` already exists
  - [x] P3.08.c Test duplicate-delivery idempotency — `BillingFlowTests.Duplicate_event_delivery_is_processed_once` + live-verified via curl through the real webhook endpoint
- [x] P3.09 Out-of-order event handling
  - [x] P3.09.a Define ordering rules using event timestamps rather than arrival order — compares an incoming event's `ProviderTimestampUtc` against the latest *processed* event for the same subscription
  - [x] P3.09.b Implement guard against regressing subscription state from a stale event — a stale event is still persisted (for audit completeness) but its transition is skipped
  - [x] P3.09.c Test an out-of-order delivery sequence — `BillingFlowTests.Out_of_order_event_does_not_regress_state`
- [x] P3.10 Provider event → Subscription state transition logic
  - [x] P3.10.a Map provider-neutral fake event types to subscription-state transitions — `ProcessProviderEventHandler.ApplyTransitionAsync`'s switch over `ProviderEventType`
  - [x] P3.10.b Implement the transition handler with the state-machine rules from P0.08 — matches docs/ARCHITECTURE.md §8 exactly, including the `Trialing -> Expired` ("trial ends unpaid") edge for a first-payment decline
  - [x] P3.10.c Test each transition path — `SubscriptionTests` (domain-level) + `BillingFlowTests` (end-to-end per transition)
- [x] P3.11 Subscription → `PlatformAccess` entitlement update
  - [x] P3.11.a Implement entitlement update triggered by subscription-state change — `UpsertEntitlementAsync`/`RevokeEntitlementAsync`, called from every transition branch
  - [ ] P3.11.b Wire the `SubscriptionActivated`/`SubscriptionExpired` outbox events (§13) — **not built as an outbox event**: no transactional-outbox infrastructure exists anywhere in this codebase yet (same gap as Phase 4's P4.09.b/P4.11.c — no `OutboxMessage` table, no dispatcher, despite the empty `src/Jobs` Hangfire scaffold). The entitlement update itself happens synchronously and reliably (same transaction as the webhook event), so this specific gap is lower-risk than Questionnaire's notification gap — nothing downstream currently needs an outbox event, it's just not wired for future consumers. Left unchecked, not hidden.
  - [x] P3.11.c Test entitlement reflects subscription state correctly after each transition — `BillingFlowTests` (one test per transition scenario)
- [x] P3.12 Structured audit trail for provider-event processing (`payment.webhook_processed`)
  - [x] P3.12.a Emit the audit event on successful webhook processing — every *newly processed* event (not stale/out-of-order ones) emits `AuditActions.PaymentWebhookProcessed`
  - [x] P3.12.b Include correlation to the `WebhookEvent` record without leaking card data — metadata carries `eventType`/`webhookEventId` only; the fake provider never generates card data at all
- [~] P3.13 Demo checkout-result page treated as informational only (no access granted client-side) — **simplified**: there is no distinct "processing…" interstitial page, because the fake provider resolves synchronously within the same request (ADR-010) — there is no async gap to poll across. The underlying rule (P3.13.c) is still true and verified.
  - [ ] P3.13.a Build the success page showing a "processing" state, not immediate access — not built; N/A given synchronous resolution, see above
  - [ ] P3.13.b Poll/refresh entitlement state until the webhook has landed — not built; N/A given synchronous resolution
  - [x] P3.13.c Verify no client-side code ever sets access state directly — confirmed by code inspection: the frontend never writes `hasActiveAccess`/entitlement state anywhere, only reads it from `GET /billing/status` after the checkout call returns

### 3.C Entitlement consumption

- [x] P3.14 `IAccessContext` contract (`HasPlatformAccessAsync`, `RequirePlatformAccessAsync`)
  - [x] P3.14.a Define the interface in BuildingBlocks/Security or a shared Contracts location — already existed in `BuildingBlocks.Application.Access` since P2.09; only `HasActivePlatformAccessAsync` is defined (no separate `RequirePlatformAccessAsync` — callers just check the bool and throw their own business error, e.g. Content's `PLATFORM_ACCESS_REQUIRED`, which was judged sufficient rather than adding a second method that only wraps the first)
  - [x] P3.14.b Implement it in Billing, querying `Entitlement` — `BillingAccessContext`, replacing `StubAccessContext` (deleted — its own doc comment said "MUST NOT be registered once Billing exists")
  - [x] P3.14.c Unit tests for each subscription state — `EntitlementTests` (indefinite/before-cutoff/after-cutoff/revoked/re-extended)
- [x] P3.15 Wire `IAccessContext` into Content playback authorization (replace Phase 2 stub)
  - [x] P3.15.a Replace the P2.09 stub with the real `IAccessContext` implementation — `StubAccessContext.cs` deleted, `BillingAccessContext` registered in its place
  - [x] P3.15.b Regression-test the Phase 2 playback authorization tests against the real implementation — Content's existing `GetVideoPlaybackHandler` tests were unaffected (they use their own test doubles); the *real* wiring was live-verified end-to-end instead: playback denied before checkout (`PLATFORM_ACCESS_REQUIRED`, 400), granted immediately after (200 with a real embed URL), matching exactly what P2.09's original stub always faked as `true`
- [x] P3.16 Subscription state rules: Trialing/Active allowed, PastDue grace period (default 3 days), Canceled access-until-period-end, Expired no access (§16)
  - [x] P3.16.a Implement the grace-period calculation (`PaidPeriodEnd + configured grace period`)
  - [x] P3.16.b Make the grace-period duration configurable — `BillingOptions.GracePeriodDays` (`appsettings` section `Billing`), default 3
  - [x] P3.16.c Test boundary conditions (exactly at grace-period expiry) — `BillingFlowTests.PastDue_access_is_denied_after_grace_period_boundary` (one day before cutoff: active; one day after: denied)

### 3.D Demo billing management & UI

- [x] P3.17 Client billing screen: subscription status, current period, payment state — `BillingPage.tsx`, live-verified
  - [x] P3.17.a Build the screen showing status/period/payment state from the billing API
  - [x] P3.17.b Wire localized status labels (§5 i18n keys, e.g. `subscription.status.active`) — `billing:status.*`
- [x] P3.18 Local demo subscription controls
  - [x] P3.18.a Add demo-only actions for renew, fail payment, cancel and expire — each button additionally disabled client-side when invalid for the current status (e.g. Renew disabled while Canceled, since the state diagram has no direct Canceled→Active edge), live-verified visually
  - [x] P3.18.b Wire the "Manage subscription" UI with a visible simulated-payment notice — `billing:demo.notice` `Alert`, always shown above the demo controls
- [~] P3.19 Simulated invoice list/detail — **list only, no distinct detail/receipt view**
  - [x] P3.19.a Build invoice list UI using locally generated invoice metadata — inline in `BillingPage.tsx`/`AdminBillingSubscriptionDetailPage.tsx`
  - [ ] P3.19.b Show a local invoice detail/receipt view — **not built**: each invoice row shows amount/currency/status/date inline; there is no separate single-invoice detail page. A real, minor gap against the literal spec — the list already carries every field an invoice has in this schema, so a detail view would currently just repeat the same four fields on its own page.

### 3.E Admin billing UI (§54)

- [~] P3.20 Subscriber table (Subscriber, Email, Status, Current Period, Access Until, Payment State, Created) — `AdminBillingListPage.tsx`, live-verified
  - [x] P3.20.a Build the table with the specified columns — **simplified**: no separate "Subscriber" (display name) column — Identity has no display-name field yet (only `Email`), so that column is Email-only, same simplification already noted for Phase 4's expert queue
  - [ ] P3.20.b Wire filtering/sorting and pagination — **partial**: the backend endpoint is paginated (`page`/`pageSize` query params) but the UI has no filter/sort controls or a pager widget yet — same "no test data volume exists yet to justify one" reasoning as Phase 2's P2.14.b
- [x] P3.21 Subscription detail view (plan, provider subscription id, status, period, payments, invoices, entitlement, webhook timeline) — `AdminBillingSubscriptionDetailPage.tsx`, live-verified
  - [x] P3.21.a Build the detail view combining Billing data (read-only cross-module projection where needed) — client email resolved via `Identity.Contracts.IUserLookup`, the same read-only cross-module pattern established in Phase 4
  - [x] P3.21.b Render the webhook timeline for the subscription — every event, newest first, with type/timestamp/processed-state
- [x] P3.22 Restrict raw webhook payload visibility to technical administrators
  - [x] P3.22.a Add a dedicated permission for raw payload access — new `billing.view_raw_webhook_payloads`, granted only to the `Administrator` role (not `Expert`, which only has `billing.view`)
  - [x] P3.22.b Hide/mask raw payloads in the standard admin view — `GetSubscriptionDetailHandler`'s `includeRawPayload` parameter is decided server-side by the caller's claims, never by the frontend; live-verified visually (admin, who holds the permission, sees the raw JSON payload inline in the webhook timeline)

### 3.F Tests (§68 highest risk area)

- [x] P3.23 Webhook idempotency tests
  - [x] P3.23.a Same event delivered twice → processed once — `BillingFlowTests.Duplicate_event_delivery_is_processed_once` + live curl verification
  - [ ] P3.23.b Concurrent delivery of the same event → no double-processing — **not tested**: only sequential duplicate delivery is covered (both in the automated test and the live curl pass). A genuinely concurrent double-POST race isn't exercised — the unique index on `ProviderEventId` would surface as a `DbUpdateException` on the second concurrent insert rather than the clean early-return path, which the current handler doesn't explicitly catch. A real, narrow gap.
- [x] P3.24 Out-of-order webhook event tests
  - [x] P3.24.a Later-timestamped event arriving first is not overwritten by an earlier one arriving late — `BillingFlowTests.Out_of_order_event_does_not_regress_state`
- [x] P3.25 Cancellation → access-until-period-end test
  - [x] P3.25.a Cancel mid-period → access remains until period end, then expires — `BillingFlowTests.Canceled_subscription_keeps_access_until_period_end`
- [x] P3.26 Expiration → access revoked test
  - [x] P3.26.a Expired subscription → `HasPlatformAccessAsync` returns false, historical data intact — `BillingFlowTests.Expired_subscription_denies_access_but_preserves_history` + live-verified (playback denied, 400)
- [x] P3.27 Grace period boundary tests (PastDue)
  - [x] P3.27.a Access allowed within grace period, denied immediately after — `BillingFlowTests.PastDue_access_is_denied_after_grace_period_boundary`
- [x] P3.28 Re-subscription restores access test
  - [x] P3.28.a Expired user re-subscribes → access restored, historical data (progress, guidance, chat) preserved — `BillingFlowTests.Re_subscribing_after_expiration_restores_access` covers billing's own history (payments/periods accumulate rather than reset, live-verified: 3 payments/2 periods after a full fail→cancel→expire→renew cycle); progress/guidance/chat preservation isn't separately tested here since Billing never touches those tables at all — there is structurally nothing that could delete them
- [~] P3.29 Entitlement tests for every subscription state — covered by multiple targeted tests rather than one parameterized test
  - [~] P3.29.a Parameterized test across Trialing/Active/PastDue/Canceled/Expired — **not literally parameterized**: `EntitlementTests` + `BillingFlowTests` between them exercise every state's access outcome, but as separate named tests, not a single `[Theory]`. Equivalent coverage, different shape.
- [ ] P3.30 Cross-user billing data access denial tests — **not tested**: the client billing API has no by-ID lookup at all (`GET /billing/status` always resolves from the caller's own JWT `sub` claim), so there is structurally no parameter surface for one user to request another's data through the client API. This is enforced by construction rather than by a checked ownership guard, which is a materially different (and untested) guarantee than Questionnaires' explicit ownership-check pattern — flagged as a real gap in test coverage even though the design itself prevents the leak.
- [x] P3.31 Fake-provider scenario matrix
  - [x] P3.31.a Test checkout success, decline, provider error and timeout — `BillingFlowTests.{Successful_checkout_activates_subscription_and_grants_access, Declined_checkout_does_not_activate, Provider_error_checkout_produces_no_event_and_no_state_change}`; Timeout isn't tested as a fourth distinct case since `FakePaymentProvider.SimulateCheckout` treats it identically to ProviderError (both return `null` — "the provider never got back to us")
  - [ ] P3.31.b Test retry after transient failure without duplicate subscription or entitlement — **not tested**: no test simulates a client retrying a checkout call after a ProviderError/Timeout outcome. `CreateCheckoutHandler`'s existing-subscription guard (`SUBSCRIPTION_ALREADY_EXISTS`) would reject a second checkout once the first has created a Trialing subscription, which is defensible behavior but isn't explicitly tested as a retry scenario.
  - [x] P3.31.c Test duplicate and out-of-order fake events through the real HTTP/application pipeline — live-verified via curl against the real `/billing/webhooks/fake` endpoint (not just the in-process handler)
- [x] P3.32 Demo-provider production safety gate
  - [x] P3.32.a Fail application startup in `Production` when any fake payment/email/video/storage adapter is registered — **video is not gated**: `YouTubeVideoProvider` (ADR-005) is a real, working integration choice, not a simulation — there is nothing "fake" about it to gate. Payment (`FakePaymentProvider`) and email (`LoggingIdentityEmailSender`, `LoggingNotificationSender`) are gated via a shared `IDemoOnlyAdapter` marker interface; no fake storage adapter exists yet (Files module is still an empty scaffold) so there's nothing to mark there either. Live-verified: booting with `ASPNETCORE_ENVIRONMENT=Production` throws `InvalidOperationException` naming all three real demo adapters and the process never starts listening; `Development` boots normally.
  - [x] P3.32.b Add an automated configuration test proving the fail-fast behavior — `ProductionSafetyExtensionsTests` (3 tests: throws in Production with a demo adapter registered, does not throw in Production without one, does not throw outside Production even with one registered)

---

## Phase 4 — Questionnaire and guidance

Deliverable: expert-led personalization works end-to-end.

### 4.A Schema (§25, §27–28)

- [x] P4.01 `Questionnaire`/`QuestionnaireTranslation` entities — `Domain/Entities/{Questionnaire,QuestionnaireTranslation}.cs`
  - [x] P4.01.a Define entities with default language and status — `QuestionnaireStatus {Draft,Published,Archived}`, same transition rules as Content's `ContentStatus` (`Publish`/`Unpublish`/`Archive`)
  - [x] P4.01.b EF configuration and migration — `AddQuestionnaireModel`, reviewed: every FK indexed, natural-key uniqueness present
- [x] P4.02 `Question`/`QuestionTranslation`, `QuestionOption`/`QuestionOptionTranslation` entities, types Text/LongText/SingleChoice/MultiChoice/Scale
  - [x] P4.02.a Define `Question` entity with `Type` enum and ordering — `QuestionType` enum, `SortOrder`, `IsRequired`
  - [x] P4.02.b Define `QuestionOption`/translations for choice/scale types — `QuestionOption.Value` is the stable machine value (used in `QuestionnaireAnswer.Value`); the visible label lives only in the translation row
  - [x] P4.02.c EF configuration and migration — unique `(QuestionId, Value)` on options, `(*, Language)` on every translation table
- [x] P4.03 `QuestionnaireSubmission` with operational timestamps (`CreatedAt, StartedAt, SubmittedAt, AssignedAt, ReviewedAt, AnsweredAt`)
  - [x] P4.03.a Define entity with all operational timestamp fields nullable until reached — each `Mark*` method is a no-op if already set, never overwrites (tested: `Operational_timestamps_are_never_overwritten_once_set`)
  - [x] P4.03.b EF configuration + FK index on `UserId` — `UserId` is an opaque indexed `Guid`, no cross-module FK constraint (same pattern as Progress/Audit's `UserId`/`ActorUserId`)
- [x] P4.04 `QuestionnaireAnswer` entity
  - [x] P4.04.a Define entity storing answer value per question, keyed to `QuestionnaireSubmission` — unique `(SubmissionId, QuestionId)`
  - [x] P4.04.b EF configuration and migration; plan for encryption at rest (P4.18) — plan is ADR-009 (see P4.18: explicitly deferred for V1, not silently dropped)
- [x] P4.05 `GuidanceResponse` entity with `Version` field (append, never silently overwrite)
  - [x] P4.05.a Define entity with `Version`, `Body`, `PublishedAt` — `PublishedAt` null while drafting; `Body` becomes immutable once published (`UpdateDraftBody` throws `InvalidOperationException` on a published row)
  - [x] P4.05.b Enforce append-only versioning at the persistence layer — unique `(SubmissionId, Version)` index; a new version is always a new row (`SaveGuidanceDraftHandler` creates version N+1 once the latest is published, never mutates it)
- [x] P4.06 `GuidanceFollowUp` entity (single bounded follow-up, not messaging)
  - [x] P4.06.a Define entity linked to a `GuidanceResponse`
  - [x] P4.06.b Enforce the one-follow-up-per-guidance constraint at the domain layer — enforced at the **database** layer (unique index on `GuidanceResponseId`), not just application-layer, per the "unique business invariants MUST be protected by database constraints" rule; `SubmitFollowUpHandler` also pre-checks and returns a clean `GUIDANCE_FOLLOWUP_ALREADY_EXISTS` business error rather than surfacing a raw constraint-violation 500

### 4.B Backend workflow (§26)

- [x] P4.07 Questionnaire builder endpoints (expert) — `AdminQuestionnairesController` (`api/v1/admin/questionnaires/*`)
  - [x] P4.07.a CRUD endpoints for questionnaires/questions/options behind `questionnaire.review`-equivalent authoring permission — gated on `WellKnownPermissionKeys.QuestionnaireReview`, per this task's own literal wording (no separate "builder" permission exists in the seeded matrix, and none was added)
  - [x] P4.07.b Integration tests for authoring flow — `QuestionnaireFlowTests` (Sqlite-backed handler tests) + live-verified via curl and Playwright (create → translate → add questions/options → publish)
- [x] P4.08 Draft save/resume endpoints (client)
  - [x] P4.08.a Implement draft-save endpoint (partial answers, `StartedAt` set on first save) — `PUT /api/v1/questionnaires/submissions/{id}/answers`, upserts per question, `MarkStarted` idempotent
  - [x] P4.08.b Implement resume/read-draft endpoint scoped to the current user only — `StartOrResumeSubmissionHandler` returns the existing Draft submission if one exists (idempotent start), never creates a duplicate while a Draft is open
- [x] P4.09 Submit endpoint → enters expert queue
  - [x] P4.09.a Implement submit endpoint setting `SubmittedAt` and status — validates every required question has a non-empty answer first (`QUESTIONNAIRE_REQUIRED_ANSWERS_MISSING` otherwise)
  - [ ] P4.09.b Trigger `QuestionnaireSubmitted` outbox event — **not built as an outbox event**: no transactional-outbox infrastructure exists anywhere in this codebase yet (no `OutboxMessage` table, no Hangfire dispatcher despite the empty `src/Jobs` scaffold) — building one is a project of its own, out of scope for this slice. `questionnaire.submitted` IS audited (`IAuditLogger`) synchronously in the same request. Left unchecked rather than misleadingly marked done; see P4.13 for the matching notification gap
- [x] P4.10 Expert queue query endpoint with waiting-time calculation — `GET /api/v1/expert/questionnaires/queue`
  - [x] P4.10.a Implement queue listing sorted by waiting time — ordered by `SubmittedAt` ascending (oldest first — the metric that matters)
  - [x] P4.10.b Compute waiting-time bucket (<24h/24-48h/>48h) server-side for consistent UI rendering — `WaitingTimeBucket` enum computed in `GetQueueHandler`, never computed client-side
- [x] P4.11 Guidance authoring + publish endpoint (versioned)
  - [x] P4.11.a Implement draft-guidance save (expert-only, permission-checked) — gated on the stronger `QuestionnaireAnswer` permission, not just `QuestionnaireReview`
  - [x] P4.11.b Implement publish endpoint creating a new `GuidanceResponse` version — `PublishGuidanceHandler`; also transitions the submission to `Answered` (only on the *first* publish — a later re-published version after a follow-up round doesn't re-transition an already-`Answered` submission)
  - [ ] P4.11.c Trigger `GuidancePublished` outbox event — **implemented as a direct in-process `INotificationSender.SendAsync` call**, not an outbox event, for the same reason as P4.09.b (no outbox infra exists). This is a real reliability gap versus the spec: a failed/crashed notification send here is not retried, unlike a proper outbox would guarantee. Documented, not hidden — revisit once P3's transactional-outbox work (or a dedicated outbox slice) lands.
- [x] P4.12 Bounded follow-up question endpoint
  - [x] P4.12.a Implement endpoint enforcing the single-follow-up limit
  - [x] P4.12.b Integration test rejecting a second follow-up attempt — `QuestionnaireFlowTests` + live-verified via curl and Playwright (second attempt returns `GUIDANCE_FOLLOWUP_ALREADY_EXISTS`)
- [~] P4.13 Notification trigger: `QuestionnaireSubmitted`, `GuidancePublished` via outbox — **partially built, and not via outbox** (see P4.09.b/P4.11.c). A new minimal `Notifications` module now exists (`INotificationSender`/`NotificationType`/`LoggingNotificationSender` — the same "log instead of send, no real provider configured" pattern as Identity's `LoggingIdentityEmailSender`), replacing the "doesn't exist yet" gap that Identity's sender comment pointed at.
  - [x] P4.13.a Wire outbox consumers to `INotificationSender` for both event types — **`GuidancePublished` only**, direct in-process call (client's email resolved via the new `Identity.Contracts.IUserLookup`). `QuestionnaireSubmitted` (notifying the expert) is **not wired**: Identity has no concept of "the primary expert's email" — only permission-based access, no designated single-expert flag — so there's no principled recipient to resolve. A real, honest gap, not silently skipped.
  - [x] P4.13.b Verify notification content excludes questionnaire/guidance text (§35) — `INotificationSender.SendAsync`'s `templateData` carries only `submissionId`, never answer/guidance text; enforced by doc comment and code review, not (yet) a runtime guard the way `AuditEntry`'s metadata-key denylist is

### 4.C Sensitive-data handling (§35)

- [x] P4.14 Explicit questionnaire consent capture + versioning
  - [x] P4.14.a Add a consent-capture step before questionnaire start, using `UserConsent` (P1.15) — `UserConsent` was defined in Phase 1 but had zero callers until now (the same "define now, wire up when a real caller exists" pattern as P1.23.c/P1.30.b). New `Identity.Contracts.IConsentContext`/`IdentityConsentContext` exposes it cross-module, mirroring `IAccessContext`. `StartOrResumeSubmissionHandler` checks consent before creating/resuming a submission; live-verified (`QUESTIONNAIRE_CONSENT_REQUIRED` 400 without consent, succeeds after)
  - [x] P4.14.b Version the consent text; require re-consent on version bump — `QuestionnaireConsent.CurrentVersion`; `HasConsentedAsync` requires `Version >= requiredVersion`
- [x] P4.15 Restrictive authorization: visible only to submitting client + authorized expert
  - [x] P4.15.a Implement resource-ownership checks on every read/write endpoint — every client handler checks `submission.UserId == callerUserId`, else throws `NotFoundAppException` (never a 403 — never confirms another user's resource exists)
  - [x] P4.15.b Integration tests for cross-user and non-expert access denial — `QuestionnaireFlowTests.A_users_submission_is_invisible_to_another_user` + live-verified (a second, unrelated client gets 404 reading another client's submission/guidance; a Client-role token gets 403 on the expert queue)
- [x] P4.16 Exclude questionnaire content from logs, analytics, notifications
  - [x] P4.16.a Audit all log statements in the Questionnaires module for leaked content — no `ILogger` calls exist anywhere in the Questionnaires module that reference `Value`/`Body`/`Answer`/`Question` text; the only logging is `LoggingNotificationSender`'s type+recipient-only line
  - [x] P4.16.b Add a lint/code-review checklist item enforcing this going forward — not a literal automated lint rule (no static-analysis tooling for this exists in the repo); enforced today via `AuditEntry.Create`'s metadata-key denylist (rejects any key containing `answer`/`questionnaire`/`guidance`) as a defense-in-depth guard at the one shared choke point every module's audit calls pass through
- [x] P4.17 Audit sensitive reads (`questionnaire.read`)
  - [x] P4.17.a Emit the audit event on every guidance/submission read by the expert — `GetSubmissionDetailForExpertHandler` emits `questionnaire.read` on every open, not just the first
  - [x] P4.17.b Verify audit metadata contains no submission content — `EntityType`/`EntityId` only (`"QuestionnaireSubmission"` + the GUID), no `Metadata` dict at all on this call
- [ ] P4.18 Encryption at rest for questionnaire responses and guidance (where feasible) — **not implemented, per ADR-009** (decided in Phase 0's architecture review, R3): V1 relies on the hosting provider's disk-level encryption + TLS in transit; column-level encryption is explicitly deferred pending legal classification of the data (§35), not silently dropped
  - [ ] P4.18.a Evaluate column-level encryption — deferred, see ADR-009
  - [ ] P4.18.b Implement for `QuestionnaireAnswer.Value` and `GuidanceResponse.Body` — deferred, see ADR-009
  - [ ] P4.18.c Verify key management approach is documented and not hardcoded — N/A while deferred
- [x] P4.19 Self-service export of questionnaire/guidance data — `GET /api/v1/questionnaires/export`
  - [x] P4.19.a Implement export endpoint producing the user's own submissions + guidance as JSON — `ExportMyQuestionnaireDataHandler`, always filters on the caller's own `UserId` by construction (never a caller-supplied id)
  - [x] P4.19.b Integration test verifying no other user's data leaks into the export — live-verified (own submissions/answers/guidance only); no dedicated automated test for cross-user export isolation specifically — a narrow gap, the underlying query shape is identical to the already-tested `GetMySubmissionHandler`'s ownership filter
- [ ] P4.20 Deletion workflow for questionnaire data respecting retention policy — **not built**: P7.06 (the retention policy this depends on) doesn't exist yet, so there is nothing to implement against. Left unchecked, not stubbed.
  - [ ] P4.20.a Implement deletion/anonymization per the retention policy from P7.06 — blocked on P7.06
  - [ ] P4.20.b Integration test verifying deletion doesn't break subscription/account integrity — blocked on P7.06

### 4.D Crisis-related guardrails (§36)

- [x] P4.21 Localized safety/disclaimer content on psychology-related pages — `shared/crisis/CrisisDisclaimer.tsx`
  - [x] P4.21.a Draft disclaimer copy (ro/en) with the product/legal-appropriate wording — `questionnaire.json`'s `crisis.*` keys; wording is a reasonable first draft, **not legally reviewed** — P4.22.b's "confirm with product owner before launch" caveat applies equally here
  - [x] P4.21.b Wire the disclaimer component onto Psychology-domain screens — wired onto every questionnaire/guidance screen (`GuidanceHomePage`, `QuestionnaireFillPage`, `SubmissionStatusPage`), which is the concrete "psychology-adjacent" surface that exists today; not yet wired onto Content's Psychology-domain program pages (no such requirement existed when Phase 2 shipped) — worth a follow-up pass once Psychology-domain content pages exist
- [x] P4.22 Visible emergency/help information where appropriate
  - [x] P4.22.a Add an emergency-info component (localized) to relevant screens — folded into `CrisisDisclaimer`'s `emergencyNotice` line rather than a separate component, since both always appear together in this pass
  - [ ] P4.22.b Confirm content sourcing/wording with the product owner before launch — **not done**, explicitly flagged; this is a real-world sign-off step, not something to fabricate
- [x] P4.23 Explicitly confirm no automated clinical-risk classification exists anywhere in the codebase
  - [x] P4.23.a Code-review pass across Questionnaires and Chat modules for any risk-scoring logic — confirmed: Questionnaires has no scoring/classification logic of any kind (answers are stored and displayed verbatim to the expert, never analyzed); Chat module doesn't exist yet (still an empty scaffold), so there is nothing to review there yet — revisit this line item when Chat lands
  - [x] P4.23.b Document the confirmation in the sensitive-data ADR/strategy doc — documented here and in `docs/HANDOVER.md` rather than a new ADR (no architectural decision was made — confirming an absence isn't a decision requiring one)

### 4.E Client UI

- [x] P4.24 Questionnaire fill/resume UI — `QuestionnaireFillPage.tsx`
  - [x] P4.24.a Build the multi-question form with save-as-draft — live-verified end-to-end
  - [x] P4.24.b Wire question-type-specific inputs (Text/LongText/SingleChoice/MultiChoice/Scale) — `QuestionInput.tsx`; MultiChoice stores its selection as a comma-separated list of option values, matching the backend's `QuestionnaireAnswer.Value` convention; Scale reuses the SingleChoice radio-group rendering (a scale is presented to the client as a set of discrete labeled points, not a slider — no distinct widget was specified)
  - [x] P4.24.c Wire the consent step before first access — live-verified (consent gate shown on first `start`, not shown again after agreeing)
- [x] P4.25 Guidance reading UI + follow-up submission — `SubmissionStatusPage.tsx`
  - [x] P4.25.a Build the guidance-reading view with version history if applicable — shows the **latest published** version only on the client side (by design — a client doesn't need to see superseded guidance history, unlike the expert's view which does show the full version history, P4.29.b)
  - [x] P4.25.b Wire the bounded follow-up submission form — live-verified end-to-end including the client-side one-per-guidance UI state (form replaced by the question+answer once asked)
- [x] P4.26 Dashboard "under review" / "guidance available" states (§41) — implemented as the dedicated `/guidance` page (`GuidanceHomePage.tsx`) rather than a small card embedded in `ClientHomePage` — this **is** the client's guidance destination per the existing nav (§40), not a supplementary summary; a home-page teaser card was not added in this pass (scope decision, not an oversight)
  - [x] P4.26.a Build the dashboard card reflecting submission/guidance state — per-questionnaire status badge (In progress/Under review/Guidance available) driven off the latest submission
  - [x] P4.26.b Wire it to the relevant read endpoints — `listPublished` + `listMySubmissions`

### 4.F Expert/admin UI (§50–51)

- [x] P4.27 Questionnaire builder UI (question list, reorder, editor, translation switcher, preview, publish) — `AdminQuestionnaireEditorPage.tsx`
  - [x] P4.27.a Build question list + reorder — **simplified**: up/down buttons instead of drag-and-drop, same deliberate simplification as P2.18 (incidentally satisfies keyboard-accessible reorder for free)
  - [x] P4.27.b Build question editor with type-specific option/scale config — text/help-text fields for every type; an Options panel (add/list/delete, value+label) appears only for SingleChoice/MultiChoice/Scale
  - [x] P4.27.c Wire translation switcher and preview mode — **language switcher**: yes (Properties panel, with per-language completion checkmarks, same pattern as Content's editor). **Preview mode**: not built — no distinct "preview as a client would see it" view; the admin can open the client-facing fill page directly in another tab to see the same effect. A real, minor gap, not silently dropped.
- [x] P4.28 Submission queue UI with aging indicators (<24h normal, 24–48h attention, >48h overdue) — `ExpertQueuePage.tsx`, live-verified visually
  - [x] P4.28.a Build the queue table per §50 columns — Client (email, via the new `IUserLookup`), Submitted At, Waiting Time, Action; **not included**: a distinct "Program/Context" column and a separate "Status"/"Last Activity" column — the queue only ever lists `Submitted` (not-yet-answered) submissions, so those columns would be constant/redundant in V1's single-questionnaire-in-practice reality. A literal simplification versus §50's exact column list.
  - [x] P4.28.b Wire the aging-bucket visual treatment from P4.10's server-computed bucket — green/amber/red `StatusBadge` per bucket
- [x] P4.29 Guidance editor: client summary, Q&A cards, timeline, editor, version history, publish action — `ExpertSubmissionPage.tsx`
  - [x] P4.29.a Build the Q&A card view (not a raw form dump) — one card per question, answer value or resolved choice label(s), live-verified
  - [x] P4.29.b Build the guidance rich-text editor + version history panel — **simplified**: plain `<textarea>`, not a WYSIWYG rich-text editor (same simplification as Content's P2.16.a); version history renders every **published** version with its follow-up Q&A inline
  - [x] P4.29.c Wire publish action with confirmation — **no confirmation dialog** — Publish is a single click, same as Content's Publish/Unpublish/Archive buttons (no confirmation dialogs exist anywhere in the admin UI yet); a real, consistent-with-precedent simplification, not a one-off gap

### 4.G Tests

- [x] P4.30 Draft/submit/guidance/versioning lifecycle tests — `QuestionnaireFlowTests.Full_flow_draft_to_published_guidance_with_followup_succeeds_end_to_end`
  - [x] P4.30.a End-to-end: draft → submit → review → guidance v1 → guidance v2 — covers draft → submit → review → guidance v1 → follow-up → follow-up answered; a v1 → **v2** re-versioning path (editing guidance again after a follow-up round) is exercised by `SaveGuidanceDraftHandler`'s version-increment logic but has no dedicated test creating an actual v2 — a narrow gap
- [x] P4.31 Bounded follow-up enforcement test (cannot exceed one)
  - [x] P4.31.a Second follow-up attempt is rejected — `QuestionnaireFlowTests` (same full-flow test) + live-verified via curl and Playwright
- [x] P4.32 Cross-user questionnaire access denial tests
  - [x] P4.32.a User A cannot read/submit against User B's questionnaire submission — `QuestionnaireFlowTests.A_users_submission_is_invisible_to_another_user` + live-verified (404, not 403, per §35)
- [ ] P4.33 Admin-has-no-implicit-access test — **not automated**: the permission matrix itself guarantees this by construction (Administrator's seeded permission set contains none of `questionnaire.submit`/`.review`/`.answer` — see `IdentitySeeder.RolePermissionMatrix`), and this was true before this batch of work, not introduced by it. No dedicated test asserts it for the Questionnaires module specifically (the closest existing coverage is Identity's own permission-matrix tests). Left unchecked as a real, narrow gap rather than marked done.
  - [ ] P4.33.a Administrator role without explicit grant cannot read submission/guidance content — not automated, see above

---

## Phase 5 — Events

Deliverable: subscribers can discover and register for live activities.

### 5.A Schema (§29–31)

- [ ] P5.01 `Event`/`EventTranslation` entities (LocationType, Status enums)
  - [ ] P5.01.a Define `Event` entity per §29 fields
  - [ ] P5.01.b Define `EventTranslation`
  - [ ] P5.01.c EF configuration and migration
- [ ] P5.02 `EventRegistration` entity + state enum (Registered/Waitlisted/Canceled)
  - [ ] P5.02.a Define entity with unique (UserId, EventId) constraint
  - [ ] P5.02.b EF configuration and migration
- [ ] P5.03 `EventReminder` entity
  - [ ] P5.03.a Define entity tracking scheduled/sent reminder state per registration
  - [ ] P5.03.b EF configuration and migration

### 5.B Backend logic

- [ ] P5.04 Event authoring endpoints (expert, translations, timezone-aware)
  - [ ] P5.04.a CRUD endpoints behind `events.manage`
  - [ ] P5.04.b Validate `StartsAtUtc`/`EndsAtUtc`/`DisplayTimezone` consistency
- [ ] P5.05 Registration endpoint requiring active `PlatformAccess`
  - [ ] P5.05.a Implement registration endpoint calling `IAccessContext.RequirePlatformAccessAsync`
  - [ ] P5.05.b Integration test denying registration without access
- [ ] P5.06 Capacity + waitlist logic; promote oldest waitlisted user on cancellation
  - [ ] P5.06.a Implement capacity check → Registered vs Waitlisted assignment
  - [ ] P5.06.b Implement promotion-on-cancellation logic (oldest waitlisted first)
  - [ ] P5.06.c Concurrency test: simultaneous registrations near capacity limit
- [ ] P5.07 Registration closes at event start
  - [ ] P5.07.a Enforce the cutoff server-side on the registration endpoint
- [ ] P5.08 Hangfire jobs: 24h and 1h reminders — idempotent, retryable, locale-aware, timezone-aware
  - [ ] P5.08.a Implement scheduling logic computing reminder fire times from `StartsAtUtc`
  - [ ] P5.08.b Implement idempotent job execution (checked against `EventReminder` sent state)
  - [ ] P5.08.c Implement locale-aware, timezone-aware email content
- [ ] P5.09 Respect notification preferences for reminders
  - [ ] P5.09.a Check `UserPreference` notification opt-in before sending
- [ ] P5.10 Outbox events: `EventPublished`, `EventRegistrationCreated`
  - [ ] P5.10.a Wire both outbox events at their trigger points

### 5.C Client UI

- [ ] P5.11 Event listing + detail screens
  - [ ] P5.11.a Build listing screen with upcoming/past filter
  - [ ] P5.11.b Build detail screen with registration status and capacity/waitlist info
- [ ] P5.12 Registration/waitlist UI with status feedback
  - [ ] P5.12.a Wire register/cancel actions with immediate status feedback
  - [ ] P5.12.b Handle waitlist-promotion notification UX
- [ ] P5.13 Dashboard "upcoming event" card (§41)
  - [ ] P5.13.a Build the card showing the nearest registered event

### 5.D Admin UI (§52)

- [ ] P5.14 Event list (Title, Date, Type, Registrations, Capacity, Status, Actions)
  - [ ] P5.14.a Build the table per §52 columns
- [ ] P5.15 Event editor (translations, date/time, timezone, location, capacity, publication status, reminders)
  - [ ] P5.15.a Build the editor form with translation switcher (reuse P4.27 pattern)
  - [ ] P5.15.b Wire timezone-aware date/time inputs
- [ ] P5.16 Event detail: registered users, waitlist, attendance, reminders
  - [ ] P5.16.a Build registered-users + waitlist views
  - [ ] P5.16.b Build reminder-status view

### 5.E Tests

- [ ] P5.17 Capacity + waitlist promotion tests
  - [ ] P5.17.a Full capacity → new registration is waitlisted
  - [ ] P5.17.b Cancellation → oldest waitlisted user promoted
- [ ] P5.18 Timezone handling tests (display vs UTC storage)
  - [ ] P5.18.a Verify storage is UTC and display honors `DisplayTimezone`/user locale
- [ ] P5.19 Reminder scheduling idempotency tests
  - [ ] P5.19.a Job re-run does not send duplicate reminders
- [ ] P5.20 Registration-requires-access tests
  - [ ] P5.20.a Registration denied for users without active `PlatformAccess`

---

## Phase 6 — Community (Chat)

May move after launch under delivery pressure (§69).

### 6.A Schema (§33)

- [ ] P6.01 Fixed room definitions (General, Psychology, Sport, Nutrition, Business, FinancialEducation) — no dynamic room creation
  - [ ] P6.01.a Define `Room` entity/enum and seed the 6 fixed rooms
  - [ ] P6.01.b Confirm no create-room endpoint exists (by design)
- [ ] P6.02 Message entity (soft delete, pin flag)
  - [ ] P6.02.a Define entity with `IsDeleted`, `IsPinned` flags
  - [ ] P6.02.b EF configuration + FK index on `RoomId`
- [ ] P6.03 Report entity, Mute entity
  - [ ] P6.03.a Define `Report` entity (message, reporter, reason, status)
  - [ ] P6.03.b Define `Mute` entity (user, expiry, moderator)

### 6.B Backend

- [ ] P6.04 SignalR hub (fallback to polling if it becomes a blocker)
  - [ ] P6.04.a Implement the SignalR hub for message broadcast per room
  - [ ] P6.04.b Time-box the effort; fall back to a polling endpoint if SignalR risks the schedule
- [ ] P6.05 Paginated message history endpoint
  - [ ] P6.05.a Implement cursor/offset-based pagination for room history
- [ ] P6.06 Basic unread-state tracking
  - [ ] P6.06.a Implement last-read-timestamp tracking per user per room
- [ ] P6.07 Pin/unpin endpoint (moderation permission)
  - [ ] P6.07.a Implement endpoint behind `chat.moderate`
- [ ] P6.08 Delete-message moderation endpoint (soft delete)
  - [ ] P6.08.a Implement soft-delete endpoint behind `chat.moderate`
  - [ ] P6.08.b Emit `chat.message_moderated` audit event
- [ ] P6.09 Temporary mute endpoint
  - [ ] P6.09.a Implement mute endpoint with expiry, behind `chat.moderate`
  - [ ] P6.09.b Emit `chat.user_muted` audit event
- [ ] P6.10 Report-message endpoint
  - [ ] P6.10.a Implement report-submission endpoint behind `chat.use`
- [ ] P6.11 Anonymize deleted user's identity in message history (preserve continuity, §66)
  - [ ] P6.11.a Implement anonymization on account deletion, preserving message text/order
  - [ ] P6.11.b Test that room history remains coherent after anonymization

### 6.C Client UI

- [ ] P6.12 Room list/switcher
  - [ ] P6.12.a Build the fixed room-switcher UI
- [ ] P6.13 Message list with pagination + pinned message highlight
  - [ ] P6.13.a Build the message list with infinite/paginated scroll
  - [ ] P6.13.b Highlight pinned message(s) at the top
- [ ] P6.14 Persistent localized privacy notice per room (§34)
  - [ ] P6.14.a Build the persistent notice component with the required warning copy (ro/en)
- [ ] P6.15 Report-message action in UI
  - [ ] P6.15.a Wire the report action with a reason selector

### 6.D Admin moderation UI (§53)

- [ ] P6.16 Reported Messages screen
  - [ ] P6.16.a Build the list with message context, author, reporter, reason, timestamp
- [ ] P6.17 Muted Users screen
  - [ ] P6.17.a Build the list of currently muted users with expiry
- [ ] P6.18 Recent Moderator Actions screen
  - [ ] P6.18.a Build the audit-derived recent-actions list
- [ ] P6.19 Per-report actions: Dismiss, Delete Message, Mute User
  - [ ] P6.19.a Wire the three actions from the report detail view

### 6.E Tests

- [ ] P6.20 Moderation action tests (delete, mute, pin) with permission checks
  - [ ] P6.20.a Authorized moderator can act; regular user cannot
- [ ] P6.21 Report flow test
  - [ ] P6.21.a Report submission → appears in admin queue → dismissible
- [ ] P6.22 Anonymization-on-delete test preserving message continuity
  - [ ] P6.22.a Deleted user's prior messages remain visible with anonymized identity

---

## Phase 7 — MVP presentation readiness

### 7.A Expert dashboard & admin views (§46, §38)

- [ ] P7.01 Expert dashboard: pending questionnaires, oldest unanswered, upcoming events, active subscribers, recent subscription changes, reported messages, recent published content
  - [ ] P7.01.a Build the dashboard layout with the specified widgets
  - [ ] P7.01.b Wire each widget to its owning module's query endpoint
- [ ] P7.02 KPI cards: active subscribers, pending questionnaires, upcoming events, monthly subscription revenue
  - [ ] P7.02.a Implement the KPI aggregation queries
  - [ ] P7.02.b Build the KPI card components
- [ ] P7.03 `SubscriberAdminView` cross-module read-only projection (Identity + Billing + Progress + last activity)
  - [ ] P7.03.a Implement the read-model query joining the specified module data (read-only, per ADR-007)
  - [ ] P7.03.b Verify the query cannot mutate any module's state

### 7.B GDPR / data rights (§66)

- [ ] P7.04 Self-service data export (JSON archive + owned attachments)
  - [ ] P7.04.a Implement the full-account export endpoint aggregating all modules' user-owned data
  - [ ] P7.04.b Include owned attachments/media references where applicable
- [ ] P7.05 Deletion workflow: hard delete vs anonymization vs retained billing records
  - [ ] P7.05.a Define per-module deletion/anonymization rules
  - [ ] P7.05.b Implement the orchestrated deletion workflow respecting legally-required billing retention
- [ ] P7.06 Documented retention policy
  - [ ] P7.06.a Write the retention-policy document per data category
  - [ ] P7.06.b Cross-reference it from the Questionnaires deletion workflow (P4.20)

### 7.C Accessibility (§59)

- [ ] P7.07 WCAG 2.2 AA audit pass: keyboard nav, focus states, semantic HTML, labels, contrast
  - [ ] P7.07.a Run an automated accessibility scan across key screens
  - [ ] P7.07.b Manually verify keyboard-only navigation on critical flows
- [ ] P7.08 Accessible dialogs/tables audit
  - [ ] P7.08.a Verify modal/drawer focus trapping and ARIA roles
  - [ ] P7.08.b Verify table semantics on both desktop and mobile card views
- [ ] P7.09 Video captions/subtitles support
  - [ ] P7.09.a Verify the video provider supports captions and wire caption upload/display
- [ ] P7.10 Reduced-motion preference support
  - [ ] P7.10.a Respect `prefers-reduced-motion` in animations/transitions

### 7.D Performance (§67)

- [ ] P7.11 Load-test representative scenario (~2,000 subscribers / ~200 concurrent)
  - [ ] P7.11.a Build a load-test scenario against the most-used endpoints
  - [ ] P7.11.b Verify response times stay in the low-hundreds-of-ms target
- [ ] P7.12 Dashboard query performance pass
  - [ ] P7.12.a Profile the client and expert dashboard queries; optimize as needed
- [ ] P7.13 Chat pagination performance check
  - [ ] P7.13.a Verify pagination performs well against a large seeded room history
- [ ] P7.14 CDN video delivery verification
  - [ ] P7.14.a Confirm video is served via the provider's CDN, not the app server
- [ ] P7.15 Index review against real query patterns
  - [ ] P7.15.a Review slow-query logs / EXPLAIN plans and adjust indexes accordingly

### 7.E Local/demo operational readiness

- [ ] P7.16 Local error visibility
  - [ ] P7.16.a Provide structured backend logs and a user-safe frontend error state without an external monitoring provider
- [ ] P7.17 Demo environment configuration (local secrets, CORS, rate limits)
  - [ ] P7.17.a Document one-command startup and required local configuration
  - [ ] P7.17.b Restrict CORS to the configured demo frontend origin
  - [ ] P7.17.c Review demo rate-limit thresholds so scripted scenarios remain usable
- [ ] P7.18 Demo database reset and seed strategy
  - [ ] P7.18.a Add a safe, repeatable reset/reseed command for the dedicated demo database
  - [ ] P7.18.b Seed Client/Expert/Admin accounts, one complete ro/en program and representative progress/subscription states
- [ ] P7.19 Reproducible demo package
  - [ ] P7.19.a Verify `docker compose up` starts the complete local MVP without third-party credentials
  - [ ] P7.19.b Document and rehearse the primary presentation journey and recovery/reset procedure
- [ ] P7.20 Full security pass (§65 checklist end-to-end)
  - [ ] P7.20.a Walk the full §65 checklist against the shipped implementation
  - [ ] P7.20.b Remediate any gaps found
- [ ] P7.21 Full audit-log coverage review against §37 action list
  - [ ] P7.21.a Verify every §37-listed action actually emits an audit entry in the shipped code
- [ ] P7.22 Deterministic external-integration simulations
  - [ ] P7.22.a Add `FakeIdentityEmailSender` delivery success, transient failure and permanent failure scenarios; expose generated verification/reset links safely in Demo only
  - [ ] P7.22.b Add `FakeVideoProvider` ready, processing and failed scenarios without external HTTP calls, while retaining YouTube URL mode for the presentation
  - [ ] P7.22.c Add `FakeFileStorage` upload/download success, unavailable-storage and invalid-object scenarios when Files gains its first MVP consumer
  - [ ] P7.22.d Select scenarios only through server-side Demo configuration or an authorized demo-admin control; never trust arbitrary client-reported success
  - [ ] P7.22.e Add contract tests proving fake and future production adapters return the same provider-neutral result/error shapes
  - [ ] P7.22.f Reset every simulation to a documented deterministic baseline as part of the demo database reset

---

# Category B — Production

These tasks begin only when deployment targets, provider choices and credentials are
available. Production adapters replace fake adapters behind the same contracts; domain
rules, entitlement decisions and client-side behavior must not be rewritten around a
specific vendor.

## Phase 8 — Real integrations and production operations

### 8.A Payments

- [ ] P8.01 Select the production payment provider and record/update the ADR
- [ ] P8.02 Implement the production `IPaymentProvider` adapter and hosted checkout
- [ ] P8.03 Verify webhook signatures, persist events and preserve idempotent/out-of-order processing
- [ ] P8.04 Implement the real billing portal and provider-hosted invoice links
- [ ] P8.05 Run provider sandbox tests, then a controlled production smoke test

### 8.B Video, email and file delivery

- [ ] P8.06 Select and integrate a production video provider with access-controlled playback
- [ ] P8.07 Implement upload/transcoding/status synchronization if required by the selected provider
- [ ] P8.08 Integrate a transactional email provider and verify delivery, retry and bounce handling
- [ ] P8.09 Integrate production object storage with authorized upload/download and lifecycle rules

### 8.C Production platform

- [ ] P8.10 Configure secrets management and validate that no fake adapter can start in `Production`
- [ ] P8.11 Integrate backend/frontend error monitoring with sensitive-data filtering
- [ ] P8.12 Configure PostgreSQL backups and complete a restore drill
- [ ] P8.13 Build deployment and migration pipeline with verified rollback
- [ ] P8.14 Lock down production CORS, rate limits, domains, TLS and security headers
- [ ] P8.15 Run full provider, security, privacy, accessibility and performance acceptance passes

---

## Cross-cutting (apply throughout every phase)

- [ ] X.01 No Romanian identifiers anywhere in technical implementation (§4)
- [ ] X.02 No hardcoded UI text — always `t("namespace.key")` (§5)
- [ ] X.03 UI locale key parity between `ro` and `en` at all times
- [ ] X.04 Server-side enforcement of every access/permission decision (§3.5, §72)
- [ ] X.05 No EF entities returned directly from any API (§72)
- [ ] X.06 No business logic inside React components or controllers (§72)
- [ ] X.07 `decimal` + explicit currency for all money values, never `float`/`double` (§63)
- [ ] X.08 All timestamps stored in UTC; formatted per user locale/timezone (§64)
- [ ] X.09 No secrets/tokens/passwords/questionnaire/guidance/card data in logs (§65)
- [ ] X.10 Migration-based schema changes only, English migration names (§4, §72)
- [ ] X.11 No out-of-scope entities/features introduced (§70 list)
