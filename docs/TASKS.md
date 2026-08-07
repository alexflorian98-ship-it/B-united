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

## Phase 0 — Architecture & Review (before any production code)

### 0.A Architecture deliverables (§74)

- [ ] P0.01 Executive architecture overview
  - [ ] P0.01.a Summarize product scope, target scale and non-goals in one page (from PROMPT.md §1–2, §67)
  - [ ] P0.01.b State the chosen architectural style and why (modular monolith, single DB) referencing ADR-001/002
  - [ ] P0.01.c List the top 3–5 architectural risks flagged for early attention
- [ ] P0.02 Module map + module ownership table
  - [ ] P0.02.a List all modules from §11 with one-line responsibility each
  - [ ] P0.02.b For each module, name the entities/tables it owns exclusively
  - [ ] P0.02.c Flag any entity whose ownership is ambiguous and resolve before Phase 1
- [ ] P0.03 Allowed cross-module dependency map
  - [ ] P0.03.a Draw a directed graph of allowed module→module Contract dependencies
  - [ ] P0.03.b Verify no cycles exist
  - [ ] P0.03.c Document the explicit exception for read-only cross-module admin projections (§12, ADR-007)
- [ ] P0.04 Domain event map (outbox events, §13)
  - [ ] P0.04.a List every outbox event: `SubscriptionActivated`, `SubscriptionExpired`, `PaymentFailed`, `QuestionnaireSubmitted`, `GuidancePublished`, `EventPublished`, `EventRegistrationCreated`
  - [ ] P0.04.b For each, document producer module, consumer(s) and the side effect triggered
  - [ ] P0.04.c Confirm no trivial synchronous call is mistakenly modeled as an outbox event
- [ ] P0.05 Full database schema (all modules)
  - [ ] P0.05.a Enumerate every table per module with columns, types and nullability
  - [ ] P0.05.b Mark PK/FK/unique constraints per table
  - [ ] P0.05.c Note money columns as `decimal` with explicit currency, timestamps as UTC
- [ ] P0.06 ER relationship overview diagram
  - [ ] P0.06.a Draw entity relationships per module cluster (Identity, Content, Billing, Questionnaires, Events, Chat)
  - [ ] P0.06.b Draw cross-module FK-like references (by ID only, no cross-module FKs in DB)
  - [ ] P0.06.c Review diagram against §18–38 entity lists for completeness
- [ ] P0.07 Key index list per table
  - [ ] P0.07.a List FK indexes required on every table (mandatory per §62)
  - [ ] P0.07.b Identify additional indexes from expected query patterns (e.g. `Subscription.Status`, `QuestionnaireSubmission.SubmittedAt`)
  - [ ] P0.07.c Explicitly reject speculative indexes not backed by a known query
- [ ] P0.08 Subscription state machine diagram (§16)
  - [ ] P0.08.a Draw states: Trialing, Active, PastDue, Canceled, Expired
  - [ ] P0.08.b Draw transitions with triggering webhook/event per transition
  - [ ] P0.08.c Annotate access-allowed vs access-denied per state, including PastDue grace period
- [ ] P0.09 Entitlement flow diagram (§15, §17)
  - [ ] P0.09.a Diagram Subscription → Entitlement (`PlatformAccess`) update flow
  - [ ] P0.09.b Diagram `IAccessContext` consumption from another module (e.g. Content)
  - [ ] P0.09.c Confirm Billing is the only writer of Entitlement records
- [ ] P0.10 Payment webhook sequence diagram (§17)
  - [ ] P0.10.a Sequence: Client → Checkout Session → Stripe → Webhook → Billing → Subscription → Entitlement
  - [ ] P0.10.b Annotate signature validation, idempotency key, and raw-event storage steps
  - [ ] P0.10.c Add the out-of-order-event handling path
- [ ] P0.11 Authentication / token lifecycle diagram (§65)
  - [ ] P0.11.a Diagram registration → email verification → login → JWT issuance
  - [ ] P0.11.b Diagram refresh-token rotation, hashing and revocation
  - [ ] P0.11.c Diagram password-reset token lifecycle and expiry
- [ ] P0.12 Permission matrix (roles × permissions, §14)
  - [ ] P0.12.a List all permission keys (content.*, questionnaire.*, events.*, chat.*, billing.*, users.manage, audit.view)
  - [ ] P0.12.b Build the Client/Expert/Administrator × permission grid
  - [ ] P0.12.c Confirm no controller-level role-string checks are implied anywhere in the design
- [ ] P0.13 Questionnaire lifecycle diagram (§26)
  - [ ] P0.13.a Diagram Draft → Submit → Expert queue → Review → Guidance → Publish → Follow-up
  - [ ] P0.13.b Annotate each operational timestamp field at its transition point
  - [ ] P0.13.c Mark the bounded-follow-up limit explicitly on the diagram
- [ ] P0.14 Progress calculation rules writeup (§23–24)
  - [ ] P0.14.a Document video 90%-watched completion rule and persistence cadence
  - [ ] P0.14.b Document rich-text manual completion rule
  - [ ] P0.14.c Document the derivation formula for program progress from Section/ContentProgress
- [ ] P0.15 Event registration / waitlist state machine (§30)
  - [ ] P0.15.a Draw states: Registered, Waitlisted, Canceled
  - [ ] P0.15.b Draw the capacity-full → waitlist and cancellation → promotion transitions
  - [ ] P0.15.c Annotate the registration-closes-at-event-start rule
- [ ] P0.16 Localization architecture writeup (§5–6)
  - [ ] P0.16.a Document the UI-locale (i18next files) vs content-locale (DB translation tables) split
  - [ ] P0.16.b Document the fallback algorithm and `translationFallbackUsed` flag
  - [ ] P0.16.c List every translatable entity pair (e.g. `Program`/`ProgramTranslation`)
- [ ] P0.17 Sensitive-data strategy writeup (§35–36)
  - [ ] P0.17.a Document access restriction rule (submitting client + authorized expert only)
  - [ ] P0.17.b Document logging/analytics/notification exclusions for questionnaire content
  - [ ] P0.17.c Document the crisis-behavior guardrails (no automated risk classification)
- [ ] P0.18 Audit strategy writeup (§37)
  - [ ] P0.18.a List all audited actions from §37
  - [ ] P0.18.b Define `AuditLog` schema and what must never be recorded (secrets, questionnaire text)
  - [ ] P0.18.c Decide read vs write audit-log access boundaries
- [ ] P0.19 Frontend route map
  - [ ] P0.19.a List client routes (Home, Programs, Program detail, Player, Events, Community, Guidance, Billing, Profile)
  - [ ] P0.19.b List expert/admin routes per §45
  - [ ] P0.19.c Mark which routes require auth vs active `PlatformAccess`
- [ ] P0.20 Client UI information architecture
  - [ ] P0.20.a Confirm navigation hierarchy per §40–41
  - [ ] P0.20.b Confirm mobile-priority nav items per §40
  - [ ] P0.20.c Map each screen to its primary data dependency (API endpoint)
- [ ] P0.21 Expert/admin UI information architecture
  - [ ] P0.21.a Confirm navigation hierarchy per §45
  - [ ] P0.21.b Map each admin screen to its owning module
  - [ ] P0.21.c Flag any screen needing a cross-module read model (§38)
- [ ] P0.22 Design-system token reference (§56)
  - [ ] P0.22.a Define the full token list (color, spacing, typography, radius, shadow, breakpoints, focus ring, motion)
  - [ ] P0.22.b Decide token naming convention and file/format (CSS variables vs Tailwind config)
  - [ ] P0.22.c Cross-check against the §55 tone requirements (no gradients, no childish illustrations)
- [ ] P0.23 Responsive rules reference (§58)
  - [ ] P0.23.a Define breakpoints for Mobile/Tablet/Laptop/Desktop
  - [ ] P0.23.b Define the table→card conversion pattern for management screens
  - [ ] P0.23.c Confirm 44px minimum touch target rule is captured in the design system
- [ ] P0.24 API contract overview (§60–61)
  - [ ] P0.24.a List all `/api/v1/*` resource groups
  - [ ] P0.24.b Define the standard error contract shape (code, messageKey, correlationId, field errors)
  - [ ] P0.24.c Define pagination/sorting/filtering conventions used across list endpoints
- [ ] P0.25 Background-job catalogue (Hangfire)
  - [ ] P0.25.a List every recurring/deferred job (event reminders, grace-period checks, notification dispatch)
  - [ ] P0.25.b Document idempotency/retry strategy per job
  - [ ] P0.25.c Document job observability requirements (logging, failure alerting)
- [ ] P0.26 Testing strategy document (§68)
  - [ ] P0.26.a Map each highest-risk area (Billing, Entitlements, Security, Questionnaires, Localization, Events, Progress) to test types (unit/integration/e2e)
  - [ ] P0.26.b Define coverage expectations for negative/authorization paths
  - [ ] P0.26.c Decide test-data and environment strategy (test DB, fixtures, seed data)
- [ ] P0.27 Deployment architecture
  - [ ] P0.27.a Diagram the single Api host + PostgreSQL + video provider + email provider topology
  - [ ] P0.27.b Document environment configuration strategy (secrets, per-environment settings)
  - [ ] P0.27.c Document backup/restore approach for PostgreSQL
- [ ] P0.28 Phase-by-phase backlog (this file)
  - [ ] P0.28.a Confirm this file's phase breakdown matches §69 delivery order
  - [ ] P0.28.b Keep task numbering stable as items are checked off
- [ ] P0.29 Architectural risks and trade-offs register
  - [ ] P0.29.a List each identified risk with likelihood/impact
  - [ ] P0.29.b Assign a mitigation or explicit "accepted risk" decision to each
  - [ ] P0.29.c Revisit this register at the end of each phase

### 0.B Architecture review (§75)

- [ ] P0.30 Challenge the proposed architecture: flag anything unnecessary, overengineered, underspecified, too tightly coupled, too generic, delay-prone or maintenance-risky
  - [ ] P0.30.a Review each module boundary for premature abstraction
  - [ ] P0.30.b Review the entitlement/outbox/localization designs specifically for over-engineering
  - [ ] P0.30.c Review §70 out-of-scope list against the architecture deliverables for scope creep
- [ ] P0.31 For each issue found: document Issue / Why it matters / Recommended change / Trade-off
  - [ ] P0.31.a Draft the issue log using the required four-field format
  - [ ] P0.31.b Prioritize issues by implementation-delay risk
- [ ] P0.32 Get explicit approval on the (possibly revised) architecture before Phase 1 implementation starts
  - [ ] P0.32.a Circulate the architecture overview + issue log for sign-off
  - [ ] P0.32.b Record the approved decisions (update ADRs where they changed)

### 0.C ADRs (§73)

- [x] P0.33 ADR-001 Modular Monolith
- [x] P0.34 ADR-002 PostgreSQL
- [x] P0.35 ADR-003 Subscription Entitlement Ownership
- [x] P0.36 ADR-004 UI vs Content Localization
- [x] P0.37 ADR-005 Video Hosting Provider Abstraction
- [x] P0.38 ADR-006 Questionnaire Sensitive Data Handling
- [x] P0.39 ADR-007 Controlled Cross-Module Read Models
- [x] P0.40 ADR-008 Transactional Outbox Usage
- [ ] P0.41 Flesh out ADR "Context" and "Consequences" sections once decisions are actually exercised in code
  - [ ] P0.41.a Revisit each ADR after its related Phase ships and fill in real Context/Consequences
  - [ ] P0.41.b Add any new ADR uncovered during the Phase 0 review (P0.30–P0.32)

---

## Phase 1 — Foundation

Deliverable: production-shaped skeleton where users can register, verify email, log in and navigate localized UI.

### 1.A Solution & infrastructure

- [ ] P1.01 Initialize .NET solution and module projects per §11 structure
  - [ ] P1.01.a Create solution file and `BuildingBlocks.*` projects
  - [ ] P1.01.b Create `Modules/<Module>/{Domain,Application,Infrastructure,Api,Contracts,Tests}` projects for all 11 modules
  - [ ] P1.01.c Create `Api`, `Jobs`, `Migrations` host projects and wire project references per the dependency map (P0.03)
  - [ ] P1.01.d Confirm `dotnet build` succeeds on a clean checkout
- [ ] P1.02 Initialize Vite + React + TypeScript app per §39 structure
  - [ ] P1.02.a Scaffold Vite + React + TS app under `frontend/`
  - [ ] P1.02.b Install core dependencies (React Router, TanStack Query, React Hook Form, Zod, Zustand, Tailwind, i18next/react-i18next)
  - [ ] P1.02.c Configure Tailwind and base folder structure to match existing scaffolding
  - [ ] P1.02.d Confirm `npm run dev` and `npm run build` succeed
- [ ] P1.03 PostgreSQL via Docker Compose, connection string configuration
  - [ ] P1.03.a Verify `docker-compose.yml` Postgres service starts and is reachable
  - [ ] P1.03.b Wire `ConnectionStrings__Default` from `.env` into the Api host configuration
  - [ ] P1.03.c Document local setup steps in `README.md`
- [ ] P1.04 EF Core base `DbContext` + per-module configuration convention (BuildingBlocks/Infrastructure)
  - [ ] P1.04.a Define base `DbContext` with shared conventions (UTC timestamps, snake_case or agreed naming)
  - [ ] P1.04.b Define `IEntityTypeConfiguration<T>` auto-registration convention per module
  - [ ] P1.04.c Add audit-timestamp interceptor (`CreatedAt`/`UpdatedAt`) shared across entities
- [ ] P1.05 Serilog structured logging setup (BuildingBlocks/Observability)
  - [ ] P1.05.a Configure Serilog sinks (console + file/structured target)
  - [ ] P1.05.b Add correlation-id enrichment middleware
  - [ ] P1.05.c Confirm sensitive-field redaction hooks exist for later modules to use (§65)
- [ ] P1.06 Health checks endpoint
  - [ ] P1.06.a Add ASP.NET health checks for the Api host
  - [ ] P1.06.b Add a PostgreSQL connectivity health check
  - [ ] P1.06.c Expose `/health` and verify it in Docker Compose
- [ ] P1.07 OpenAPI / Swagger setup
  - [ ] P1.07.a Add Swagger/OpenAPI generation to the Api host
  - [ ] P1.07.b Configure JWT bearer auth in the Swagger UI
  - [ ] P1.07.c Restrict Swagger UI exposure per environment (dev only, or gated)
- [ ] P1.08 ASP.NET rate limiting middleware
  - [ ] P1.08.a Configure global rate-limiting policy
  - [ ] P1.08.b Add a stricter policy for auth endpoints (login, password reset)
  - [ ] P1.08.c Verify rate-limit responses match the standard error contract (§61)
- [ ] P1.09 Standardized error-response middleware (§61 contract)
  - [ ] P1.09.a Implement global exception-handling middleware producing `{code, messageKey, correlationId}`
  - [ ] P1.09.b Implement FluentValidation error mapping to the field-error shape
  - [ ] P1.09.c Add tests covering unhandled-exception, validation-failure and not-found responses
- [ ] P1.10 CI foundation (build, test, lint on push)
  - [ ] P1.10.a Add CI workflow: restore, build, run backend tests
  - [ ] P1.10.b Add CI steps for frontend: install, lint, build
  - [ ] P1.10.c Fail the pipeline on build/test/lint failures
- [ ] P1.11 Base Dockerfile(s) for the Api host
  - [ ] P1.11.a Write a multi-stage Dockerfile for the Api host
  - [ ] P1.11.b Add the Api service to `docker-compose.yml` alongside PostgreSQL
  - [ ] P1.11.c Verify the containerized Api can reach PostgreSQL and serve `/health`

### 1.B Identity module (§14)

- [ ] P1.12 `User`, `Role`, `Permission`, `RolePermission`, `UserRole` entities + EF configuration
  - [ ] P1.12.a Define entity classes with invariants (e.g. unique email, normalized email casing)
  - [ ] P1.12.b Write EF Core configuration: keys, FK relationships, required fields, unique constraint on `User.Email`
  - [ ] P1.12.c Add FK indexes (`RolePermission`, `UserRole`) per §62
  - [ ] P1.12.d Generate and review the initial Identity migration
  - [ ] P1.12.e Unit tests for entity invariants
- [ ] P1.13 `RefreshToken` entity: hashed, rotating, revocable (§65)
  - [ ] P1.13.a Define entity storing only the hashed token value, expiry and revocation state
  - [ ] P1.13.b Implement token generation + hashing on issuance
  - [ ] P1.13.c Implement rotation-on-use (old token invalidated, new one issued)
  - [ ] P1.13.d Unit tests for reuse-detection (revoked token reuse should fail and optionally revoke the token family)
- [ ] P1.14 `EmailVerificationToken`, `PasswordResetToken` entities
  - [ ] P1.14.a Define entities with expiry and single-use semantics
  - [ ] P1.14.b Implement token issuance and consumption logic
  - [ ] P1.14.c Unit tests for expiry and already-used token rejection
- [ ] P1.15 `UserConsent`, `UserPreference` entities
  - [ ] P1.15.a Define `UserConsent` with consent type + version + timestamp (used later by Questionnaires §35)
  - [ ] P1.15.b Define `UserPreference` covering timezone (§64) and notification opt-in flags (§32)
  - [ ] P1.15.c EF configuration and migration
- [ ] P1.16 Seed initial roles: `Client`, `Expert`, `Administrator`
  - [ ] P1.16.a Add a seed/migration step inserting the three roles with stable IDs
  - [ ] P1.16.b Verify seed is idempotent on repeated migration runs
- [ ] P1.17 Seed initial permission set (content.*, questionnaire.*, events.*, chat.*, billing.*, users.manage, audit.view)
  - [ ] P1.17.a Enumerate the full permission-key list from P0.12
  - [ ] P1.17.b Seed permissions and default `RolePermission` grants per the permission matrix
  - [ ] P1.17.c Verify seed is idempotent
- [ ] P1.18 Registration endpoint + password hashing
  - [ ] P1.18.a Define `RegisterRequest`/`RegisterResponse` DTOs + FluentValidation
  - [ ] P1.18.b Implement password hashing (e.g. ASP.NET Identity-style hasher) — never store plaintext
  - [ ] P1.18.c Implement handler: create `User`, assign `Client` role, trigger `EmailVerification` notification
  - [ ] P1.18.d Integration tests: happy path, duplicate email, weak password
- [ ] P1.19 Email verification flow
  - [ ] P1.19.a Implement verify-email endpoint consuming `EmailVerificationToken`
  - [ ] P1.19.b Wire `Welcome` notification on successful verification
  - [ ] P1.19.c Integration tests: valid token, expired token, already-verified user
- [ ] P1.20 Login endpoint issuing JWT access token + rotating refresh token
  - [ ] P1.20.a Implement credential validation + failed-login audit event
  - [ ] P1.20.b Issue JWT access token with permission claims and a refresh token
  - [ ] P1.20.c Integration tests: valid login, invalid password, unverified email, locked account
- [ ] P1.21 Refresh-token rotation + revocation endpoint
  - [ ] P1.21.a Implement `/auth/refresh` consuming and rotating the refresh token
  - [ ] P1.21.b Implement `/auth/revoke` (logout) endpoint
  - [ ] P1.21.c Integration tests: rotation success, reuse of a revoked token, revoke-all-sessions path
- [ ] P1.22 Password reset flow
  - [ ] P1.22.a Implement request-reset endpoint (always returns success regardless of email existence)
  - [ ] P1.22.b Implement confirm-reset endpoint consuming `PasswordResetToken`
  - [ ] P1.22.c Trigger `PasswordReset` notification and audit event
  - [ ] P1.22.d Integration tests: valid flow, expired/used token, token reuse rejection
- [ ] P1.23 Permission-based authorization policies (no `if (user.Role == "Expert")` in controllers)
  - [ ] P1.23.a Implement a permission-claim authorization handler/policy provider
  - [ ] P1.23.b Register one policy per permission key from P1.17
  - [ ] P1.23.c Apply `[Authorize(Policy = "...")]` consistently; add an analyzer/lint check against role-string checks
- [ ] P1.24 Account lockout / abuse protection on auth endpoints
  - [ ] P1.24.a Implement failed-attempt counting and temporary lockout
  - [ ] P1.24.b Combine with the rate-limiting policy from P1.08
  - [ ] P1.24.c Integration tests: lockout triggers after N failures and clears after cooldown

### 1.C Localization infrastructure

- [ ] P1.25 i18next + react-i18next setup with lazy-loaded namespaces
  - [ ] P1.25.a Configure i18next with `ro` default, `en` fallback, and namespace-per-feature loading
  - [ ] P1.25.b Wire the i18next provider into the app root
  - [ ] P1.25.c Verify lazy namespace loading works on route change (no full-namespace bundle upfront)
- [ ] P1.26 Seed `ro`/`en` locale namespace files (common, auth) with real keys
  - [ ] P1.26.a Replace placeholder `common.json`/`auth.json` with real keys used by Phase 1 screens
  - [ ] P1.26.b Verify key parity between `ro` and `en`
  - [ ] P1.26.c Add a CI check (or script) that fails on key-parity mismatch
- [ ] P1.27 Language switcher component
  - [ ] P1.27.a Build the switcher UI using design-system primitives
  - [ ] P1.27.b Persist selected language to `UserPreference` (authenticated) and local storage (anonymous)
  - [ ] P1.27.c Verify switching language does not require a full page reload
- [ ] P1.28 DB-backed translation lookup infrastructure (BuildingBlocks/Localization) with default-language fallback + `translationFallbackUsed` flag pattern (used from Phase 2 onward)
  - [ ] P1.28.a Implement a generic translation-resolution helper given an entity's translations collection + requested language
  - [ ] P1.28.b Implement default-language fallback + `translationFallbackUsed` flag output
  - [ ] P1.28.c Unit tests: exact match, fallback, missing-default-language edge case

### 1.D Design system foundation

- [ ] P1.29 Design tokens (color, spacing, typography, radius, shadow, breakpoints, focus ring, motion) per §56
  - [ ] P1.29.a Implement tokens from P0.22 as Tailwind config / CSS variables
  - [ ] P1.29.b Implement light theme; confirm dark-mode approach (or explicitly defer)
  - [ ] P1.29.c Document token usage rules (no arbitrary values in components)
- [ ] P1.30 Core primitives: Button, Input, Card, Badge, Alert, Toast, Skeleton, EmptyState
  - [ ] P1.30.a Implement each primitive using tokens only, with accessible states (focus, disabled, error)
  - [ ] P1.30.b Add Storybook or equivalent visual reference (optional but recommended)
  - [ ] P1.30.c Unit/interaction tests for keyboard accessibility on interactive primitives
- [ ] P1.31 Base layouts: client layout shell, expert/admin layout shell
  - [ ] P1.31.a Build client layout with nav per §40
  - [ ] P1.31.b Build expert/admin layout with nav per §45
  - [ ] P1.31.c Verify both shells are responsive per §58

### 1.E Audit foundation

- [ ] P1.32 `AuditLog` entity + write API (BuildingBlocks or Audit module)
  - [ ] P1.32.a Define `AuditLog` entity per §37 schema
  - [ ] P1.32.b Implement a write-only append API (`IAuditLogger`) usable from any module
  - [ ] P1.32.c Verify no secrets/tokens/questionnaire text can be passed into `Metadata` (guard at the API boundary)
- [ ] P1.33 Wire audit events: `user.login`, `user.failed_login`, `user.password_reset`, `user.role_changed`
  - [ ] P1.33.a Emit `user.login`/`user.failed_login` from the login handler
  - [ ] P1.33.b Emit `user.password_reset` from the reset-confirm handler
  - [ ] P1.33.c Emit `user.role_changed` from the (future) role-assignment path; stub the call site now if the admin UI isn't built yet

### 1.F Tests

- [ ] P1.34 Auth flow tests (register, verify, login, refresh, reset)
  - [ ] P1.34.a End-to-end happy-path test: register → verify → login → refresh → logout
  - [ ] P1.34.b Negative tests: duplicate registration, wrong password, expired tokens
  - [ ] P1.34.c Token-reuse and revocation tests
- [ ] P1.35 Permission policy enforcement tests (positive + negative)
  - [ ] P1.35.a For each seeded permission, test an authorized call succeeds
  - [ ] P1.35.b For each seeded permission, test an unauthorized call is rejected (403)
  - [ ] P1.35.c Test that an unauthenticated call is rejected (401) on protected endpoints

---

## Phase 2 — Content

Deliverable: the expert can publish programs and clients can consume them.

### 2.A Domain & schema

- [ ] P2.01 `Domain` entity, seed 5 initial domains (Psychology, Sport, Nutrition, Business, FinancialEducation)
  - [ ] P2.01.a Define `Domain` entity and EF configuration
  - [ ] P2.01.b Seed the 5 domains with stable IDs/slugs
  - [ ] P2.01.c Verify seed is idempotent
- [ ] P2.02 `Program` + `ProgramTranslation` entities (§19)
  - [ ] P2.02.a Define `Program` entity (status, default language, sort order, concurrency token)
  - [ ] P2.02.b Define `ProgramTranslation` (Title, ShortDescription, Description)
  - [ ] P2.02.c EF configuration: FK to `Domain`, unique `Slug`, FK index
- [ ] P2.03 `Section` + `SectionTranslation` entities (§20)
  - [ ] P2.03.a Define `Section` entity with ordered `SortOrder` within a `Program`
  - [ ] P2.03.b Define `SectionTranslation` (Title, Description)
  - [ ] P2.03.c EF configuration + FK index on `ProgramId`
- [ ] P2.04 `ContentItem` + `ContentItemTranslation` entities, types `Video`/`RichText` (§21)
  - [ ] P2.04.a Define `ContentItem` entity with `Type` enum and `IsRequired` flag
  - [ ] P2.04.b Define `ContentItemTranslation` (Title, Body — body meaning depends on type)
  - [ ] P2.04.c EF configuration + FK index on `SectionId` and nullable FK to `MediaAsset`
- [ ] P2.05 `MediaAsset` entity + processing-status enum (§22)
  - [ ] P2.05.a Define `MediaAsset` entity (Provider, ProviderAssetId, ProviderPlaybackId, DurationSeconds, ThumbnailUrl)
  - [ ] P2.05.b Define `ProcessingStatus` enum (Uploading, Processing, Ready, Failed)
  - [ ] P2.05.c EF configuration and migration
- [ ] P2.06 Migrations for all Content tables with FK indexes
  - [ ] P2.06.a Generate the consolidated Content-module migration
  - [ ] P2.06.b Review generated indexes against P0.07
  - [ ] P2.06.c Apply and verify against a clean database

### 2.B Video provider integration

- [ ] P2.07 Video-provider abstraction interface (Mux / Cloudflare Stream / Vimeo)
  - [ ] P2.07.a Define `IVideoProvider` interface (upload, get status, issue signed playback URL)
  - [ ] P2.07.b Implement the concrete provider adapter (choose provider per ADR-005)
  - [ ] P2.07.c Configuration/secrets wiring via `.env` (`VideoProvider__*`)
- [ ] P2.08 Upload flow → provider → `MediaAsset` metadata sync
  - [ ] P2.08.a Implement upload-initiation endpoint (expert-only)
  - [ ] P2.08.b Implement provider webhook/poll to sync `ProcessingStatus` and duration/thumbnail into `MediaAsset`
  - [ ] P2.08.c Integration test: upload → processing → ready state transitions
- [ ] P2.09 Signed/short-lived playback URL issuance gated on active `PlatformAccess` (stub `IAccessContext` until Phase 3 lands)
  - [ ] P2.09.a Define a temporary `IAccessContext` stub returning true/false for local testing
  - [ ] P2.09.b Implement playback-URL endpoint calling the stub before issuing a signed URL
  - [ ] P2.09.c Add a tracked follow-up to replace the stub in Phase 3 (P3.15)

### 2.C Backend API

- [ ] P2.10 Program/Section/ContentItem CRUD endpoints (expert-only, `content.*` permissions)
  - [ ] P2.10.a Define DTOs + FluentValidation for create/update per entity
  - [ ] P2.10.b Implement handlers enforcing `content.create`/`content.edit` permissions
  - [ ] P2.10.c Integration tests: authorized CRUD, unauthorized rejection
- [ ] P2.11 Publish/unpublish/archive workflow endpoints
  - [ ] P2.11.a Implement status-transition endpoint enforcing `content.publish`
  - [ ] P2.11.b Validate allowed transitions (Draft→Published→Archived, no skipping/backwards where invalid)
  - [ ] P2.11.c Integration tests per transition
- [ ] P2.12 Client-facing read endpoints with translation fallback applied
  - [ ] P2.12.a Implement list/detail endpoints returning only `Published` programs to clients
  - [ ] P2.12.b Apply the P1.28 translation-fallback helper and expose `translationFallbackUsed` only in admin DTOs
  - [ ] P2.12.c Integration tests: fallback behavior, published-only filtering
- [ ] P2.13 Content ordering/reorder endpoints
  - [ ] P2.13.a Implement reorder endpoint for sections within a program and content items within a section
  - [ ] P2.13.b Ensure reorder is transactional and concurrency-safe
  - [ ] P2.13.c Integration tests for reorder correctness

### 2.D Admin authoring UI

- [ ] P2.14 Program list screen (All/Drafts/Published/Archived) per §47
  - [ ] P2.14.a Build the filtered list view with the columns from §47
  - [ ] P2.14.b Wire TanStack Query against the list endpoint with pagination
  - [ ] P2.14.c Add row actions: Edit, Preview, Publish/Unpublish, Duplicate, Archive
- [ ] P2.15 Three-area program editor (Structure / Editor / Properties) per §48
  - [ ] P2.15.a Build the layout shell (Structure sidebar / Editor canvas / Properties panel)
  - [ ] P2.15.b Wire section/content-item selection state between the three areas
  - [ ] P2.15.c Wire save/publish actions with optimistic UI + error handling
- [ ] P2.16 Rich text editor component
  - [ ] P2.16.a Integrate a rich-text editor library behind the design-system primitives
  - [ ] P2.16.b Wire content persistence (draft-save, explicit save)
  - [ ] P2.16.c Sanitize/validate output before it's sent to the API
- [ ] P2.17 Video configuration UI (upload trigger, processing status)
  - [ ] P2.17.a Build upload-trigger UI calling P2.08's initiation endpoint
  - [ ] P2.17.b Poll/display processing status until `Ready`/`Failed`
  - [ ] P2.17.c Handle failed-upload retry UX
- [ ] P2.18 Drag-and-drop reordering for sections/content items
  - [ ] P2.18.a Implement drag-and-drop in the Structure panel
  - [ ] P2.18.b Call the P2.13 reorder endpoint on drop with optimistic update + rollback on failure
  - [ ] P2.18.c Verify keyboard-accessible reorder fallback exists (§59)
- [ ] P2.19 Contextual translation status UI (Complete / Missing X) per §49
  - [ ] P2.19.a Build the per-language completion indicator component
  - [ ] P2.19.b Wire language switcher within the editor to load/save the selected translation
  - [ ] P2.19.c Verify missing-translation state is visually distinct

### 2.E Client UI

- [ ] P2.20 Programs screen: domain filter, program cards, CTA state (Start/Continue/Completed) per §42
  - [ ] P2.20.a Build domain filter + program card grid
  - [ ] P2.20.b Compute and display CTA state from progress data
  - [ ] P2.20.c Wire TanStack Query against the client list endpoint
- [ ] P2.21 Program detail screen per §43
  - [ ] P2.21.a Build header (cover, domain, title, description, progress, primary action)
  - [ ] P2.21.b Build section list with completion state and content count
  - [ ] P2.21.c Wire navigation into the player (P2.22)
- [ ] P2.22 Program player: desktop 3-pane layout, mobile curriculum drawer per §44
  - [ ] P2.22.a Build desktop layout (header, curriculum sidebar, content pane, prev/next footer)
  - [ ] P2.22.b Build mobile layout with curriculum drawer (not a shrunk sidebar)
  - [ ] P2.22.c Wire content-type rendering (video vs rich text) and next/previous navigation
- [ ] P2.23 Video player component with resume position
  - [ ] P2.23.a Integrate a video player against the provider's playback URL
  - [ ] P2.23.b Wire resume-from-last-position using `ContentProgress.LastVideoPositionSeconds`
  - [ ] P2.23.c Wire progress-reporting triggers (see P2.26)

### 2.F Progress tracking (§23–24)

- [ ] P2.24 `ContentProgress` entity + status enum
  - [ ] P2.24.a Define entity (Status, LastVideoPositionSeconds, WatchPercentage, StartedAt, CompletedAt)
  - [ ] P2.24.b EF configuration + unique constraint on (UserId, ContentItemId)
  - [ ] P2.24.c Migration
- [ ] P2.25 `SectionProgress` entity (denormalized for dashboards)
  - [ ] P2.25.a Define entity and recalculation trigger points
  - [ ] P2.25.b EF configuration + unique constraint on (UserId, SectionId)
  - [ ] P2.25.c Migration
- [ ] P2.26 Video auto-complete at ~90% watched + periodic (~15s) position persistence, plus pause/navigate/close/complete triggers
  - [ ] P2.26.a Implement client-side progress-reporting hook (interval + event-triggered)
  - [ ] P2.26.b Implement backend endpoint accepting position/percentage updates
  - [ ] P2.26.c Implement server-side auto-complete rule at ≥90% watched
- [ ] P2.27 Rich-text manual "Mark as completed" action
  - [ ] P2.27.a Build the UI action in the player
  - [ ] P2.27.b Implement backend endpoint setting `ContentProgress.Status = Completed`
  - [ ] P2.27.c Integration test for the manual-completion path
- [ ] P2.28 Derived program-progress calculation (no persisted `ProgramProgress` table unless proven necessary)
  - [ ] P2.28.a Implement the derivation function from Section/ContentProgress
  - [ ] P2.28.b Verify deterministic recalculation (same inputs → same output)
  - [ ] P2.28.c Benchmark on representative data; only add a persisted table if measurements justify it

### 2.G Localization content

- [ ] P2.29 `ro`/`en` UI locale entries for `content.json`
  - [ ] P2.29.a Fill in real keys for the Programs/Program detail/Player screens
  - [ ] P2.29.b Verify `ro`/`en` key parity
- [ ] P2.30 Seed at least one fully translated demo program (ro + en) for manual verification
  - [ ] P2.30.a Author one demo program with sections and both content types, translated in both languages
  - [ ] P2.30.b Verify it renders correctly end-to-end in both UI languages

### 2.H Tests

- [ ] P2.31 Translation fallback tests (missing translation → default + flag)
  - [ ] P2.31.a Test exact-language match returns correct content
  - [ ] P2.31.b Test missing translation falls back to default language with `translationFallbackUsed = true`
- [ ] P2.32 Video completion threshold tests
  - [ ] P2.32.a Test completion triggers at ≥90% watched
  - [ ] P2.32.b Test completion does not trigger below threshold
- [ ] P2.33 Video resume-position tests
  - [ ] P2.33.a Test resume position is persisted and returned correctly
  - [ ] P2.33.b Test resume position updates on pause/navigate/close triggers
- [ ] P2.34 Rich-text manual completion tests
  - [ ] P2.34.a Test manual completion sets status correctly
  - [ ] P2.34.b Test rich text is never auto-completed without explicit action
- [ ] P2.35 Playback URL authorization tests (denied without access)
  - [ ] P2.35.a Test playback URL denied when access stub returns false
  - [ ] P2.35.b Test playback URL issued (short-lived) when access stub returns true

---

## Phase 3 — Billing and access

Deliverable: only valid subscribers can access protected platform functionality.

### 3.A Schema (§15)

- [ ] P3.01 `Plan`, `PlanPrice` entities (decimal + explicit currency, §63)
  - [ ] P3.01.a Define `Plan` entity (name, description, active flag)
  - [ ] P3.01.b Define `PlanPrice` entity (`decimal` amount, explicit currency, billing interval)
  - [ ] P3.01.c EF configuration and migration
- [ ] P3.02 `Subscription`, `SubscriptionPeriod` entities + state enum (§16)
  - [ ] P3.02.a Define `Subscription` entity with `Status` enum (Trialing/Active/PastDue/Canceled/Expired)
  - [ ] P3.02.b Define `SubscriptionPeriod` entity (period start/end, paid period end)
  - [ ] P3.02.c EF configuration + FK index on `UserId`
- [ ] P3.03 `PaymentCustomer`, `Payment`, `Invoice` entities
  - [ ] P3.03.a Define `PaymentCustomer` (provider customer id ↔ `UserId`)
  - [ ] P3.03.b Define `Payment` and `Invoice` entities with `decimal` amounts and explicit currency
  - [ ] P3.03.c EF configuration and migration
- [ ] P3.04 `WebhookEvent` entity (raw event storage, unique provider event ID)
  - [ ] P3.04.a Define entity storing raw payload, provider event ID (unique), processed timestamp
  - [ ] P3.04.b EF configuration with unique constraint on provider event ID
  - [ ] P3.04.c Migration
- [ ] P3.05 `Entitlement` entity used as `PlatformAccess` in V1
  - [ ] P3.05.a Define generic `Entitlement` entity (Type, ValidFrom, ValidUntil, Status, SourceType, SourceId)
  - [ ] P3.05.b Seed/insert convention for `PlatformAccess` type
  - [ ] P3.05.c EF configuration and migration

### 3.B Stripe integration (§17)

- [ ] P3.06 Checkout Session creation endpoint
  - [ ] P3.06.a Implement endpoint creating a Stripe Checkout Session for a selected plan
  - [ ] P3.06.b Ensure the checkout-success redirect carries no access-granting side effect (informational only)
  - [ ] P3.06.c Integration test against Stripe test mode
- [ ] P3.07 Webhook endpoint: signature validation
  - [ ] P3.07.a Implement Stripe webhook endpoint with signature verification
  - [ ] P3.07.b Reject invalid-signature requests without processing
  - [ ] P3.07.c Test with valid and tampered signatures
- [ ] P3.08 Webhook idempotent processing keyed on provider event ID
  - [ ] P3.08.a Persist incoming events to `WebhookEvent` before processing
  - [ ] P3.08.b Skip processing if the event ID was already handled
  - [ ] P3.08.c Test duplicate-delivery idempotency
- [ ] P3.09 Out-of-order event handling
  - [ ] P3.09.a Define ordering rules using event timestamps rather than arrival order
  - [ ] P3.09.b Implement guard against regressing subscription state from a stale event
  - [ ] P3.09.c Test an out-of-order delivery sequence
- [ ] P3.10 Webhook → Subscription state transition logic
  - [ ] P3.10.a Map each relevant Stripe event type to a subscription-state transition
  - [ ] P3.10.b Implement the transition handler with the state-machine rules from P0.08
  - [ ] P3.10.c Test each transition path
- [ ] P3.11 Subscription → `PlatformAccess` entitlement update
  - [ ] P3.11.a Implement entitlement update triggered by subscription-state change
  - [ ] P3.11.b Wire the `SubscriptionActivated`/`SubscriptionExpired` outbox events (§13)
  - [ ] P3.11.c Test entitlement reflects subscription state correctly after each transition
- [ ] P3.12 Structured audit trail for webhook processing (`payment.webhook_processed`)
  - [ ] P3.12.a Emit the audit event on successful webhook processing
  - [ ] P3.12.b Include correlation to the `WebhookEvent` record without leaking card data
- [ ] P3.13 Checkout-success redirect treated as informational only (no access granted client-side)
  - [ ] P3.13.a Build the success page showing a "processing" state, not immediate access
  - [ ] P3.13.b Poll/refresh entitlement state until the webhook has landed
  - [ ] P3.13.c Verify no client-side code ever sets access state directly

### 3.C Entitlement consumption

- [ ] P3.14 `IAccessContext` contract (`HasPlatformAccessAsync`, `RequirePlatformAccessAsync`)
  - [ ] P3.14.a Define the interface in BuildingBlocks/Security or a shared Contracts location
  - [ ] P3.14.b Implement it in Billing, querying `Entitlement`
  - [ ] P3.14.c Unit tests for each subscription state
- [ ] P3.15 Wire `IAccessContext` into Content playback authorization (replace Phase 2 stub)
  - [ ] P3.15.a Replace the P2.09 stub with the real `IAccessContext` implementation
  - [ ] P3.15.b Regression-test the Phase 2 playback authorization tests against the real implementation
- [ ] P3.16 Subscription state rules: Trialing/Active allowed, PastDue grace period (default 3 days), Canceled access-until-period-end, Expired no access (§16)
  - [ ] P3.16.a Implement the grace-period calculation (`PaidPeriodEnd + configured grace period`)
  - [ ] P3.16.b Make the grace-period duration configurable
  - [ ] P3.16.c Test boundary conditions (exactly at grace-period expiry)

### 3.D Billing portal & UI

- [ ] P3.17 Client billing screen: subscription status, current period, payment state
  - [ ] P3.17.a Build the screen showing status/period/payment state from the billing API
  - [ ] P3.17.b Wire localized status labels (§5 i18n keys, e.g. `subscription.status.active`)
- [ ] P3.18 Stripe billing portal hand-off
  - [ ] P3.18.a Implement endpoint creating a Stripe billing-portal session
  - [ ] P3.18.b Wire the "Manage billing" UI action
- [ ] P3.19 Invoice list/download
  - [ ] P3.19.a Build invoice list UI
  - [ ] P3.19.b Wire download/link to provider-hosted invoice PDF

### 3.E Admin billing UI (§54)

- [ ] P3.20 Subscriber table (Subscriber, Email, Status, Current Period, Access Until, Payment State, Created)
  - [ ] P3.20.a Build the table with the specified columns
  - [ ] P3.20.b Wire filtering/sorting and pagination
- [ ] P3.21 Subscription detail view (plan, provider subscription id, status, period, payments, invoices, entitlement, webhook timeline)
  - [ ] P3.21.a Build the detail view combining Billing data (read-only cross-module projection where needed)
  - [ ] P3.21.b Render the webhook timeline for the subscription
- [ ] P3.22 Restrict raw webhook payload visibility to technical administrators
  - [ ] P3.22.a Add a dedicated permission for raw payload access
  - [ ] P3.22.b Hide/mask raw payloads in the standard admin view

### 3.F Tests (§68 highest risk area)

- [ ] P3.23 Webhook idempotency tests
  - [ ] P3.23.a Same event delivered twice → processed once
  - [ ] P3.23.b Concurrent delivery of the same event → no double-processing
- [ ] P3.24 Out-of-order webhook event tests
  - [ ] P3.24.a Later-timestamped event arriving first is not overwritten by an earlier one arriving late
- [ ] P3.25 Cancellation → access-until-period-end test
  - [ ] P3.25.a Cancel mid-period → access remains until period end, then expires
- [ ] P3.26 Expiration → access revoked test
  - [ ] P3.26.a Expired subscription → `HasPlatformAccessAsync` returns false, historical data intact
- [ ] P3.27 Grace period boundary tests (PastDue)
  - [ ] P3.27.a Access allowed within grace period, denied immediately after
- [ ] P3.28 Re-subscription restores access test
  - [ ] P3.28.a Expired user re-subscribes → access restored, historical data (progress, guidance, chat) preserved
- [ ] P3.29 Entitlement tests for every subscription state
  - [ ] P3.29.a Parameterized test across Trialing/Active/PastDue/Canceled/Expired
- [ ] P3.30 Cross-user billing data access denial tests
  - [ ] P3.30.a User A cannot read User B's subscription/invoice/payment data

---

## Phase 4 — Questionnaire and guidance

Deliverable: expert-led personalization works end-to-end.

### 4.A Schema (§25, §27–28)

- [ ] P4.01 `Questionnaire`/`QuestionnaireTranslation` entities
  - [ ] P4.01.a Define entities with default language and status
  - [ ] P4.01.b EF configuration and migration
- [ ] P4.02 `Question`/`QuestionTranslation`, `QuestionOption`/`QuestionOptionTranslation` entities, types Text/LongText/SingleChoice/MultiChoice/Scale
  - [ ] P4.02.a Define `Question` entity with `Type` enum and ordering
  - [ ] P4.02.b Define `QuestionOption`/translations for choice/scale types
  - [ ] P4.02.c EF configuration and migration
- [ ] P4.03 `QuestionnaireSubmission` with operational timestamps (`CreatedAt, StartedAt, SubmittedAt, AssignedAt, ReviewedAt, AnsweredAt`)
  - [ ] P4.03.a Define entity with all operational timestamp fields nullable until reached
  - [ ] P4.03.b EF configuration + FK index on `UserId`
- [ ] P4.04 `QuestionnaireAnswer` entity
  - [ ] P4.04.a Define entity storing answer value per question, keyed to `QuestionnaireSubmission`
  - [ ] P4.04.b EF configuration and migration; plan for encryption at rest (P4.18)
- [ ] P4.05 `GuidanceResponse` entity with `Version` field (append, never silently overwrite)
  - [ ] P4.05.a Define entity with `Version`, `Body`, `PublishedAt`
  - [ ] P4.05.b Enforce append-only versioning at the persistence layer
- [ ] P4.06 `GuidanceFollowUp` entity (single bounded follow-up, not messaging)
  - [ ] P4.06.a Define entity linked to a `GuidanceResponse`
  - [ ] P4.06.b Enforce the one-follow-up-per-guidance constraint at the domain layer

### 4.B Backend workflow (§26)

- [ ] P4.07 Questionnaire builder endpoints (expert)
  - [ ] P4.07.a CRUD endpoints for questionnaires/questions/options behind `questionnaire.review`-equivalent authoring permission
  - [ ] P4.07.b Integration tests for authoring flow
- [ ] P4.08 Draft save/resume endpoints (client)
  - [ ] P4.08.a Implement draft-save endpoint (partial answers, `StartedAt` set on first save)
  - [ ] P4.08.b Implement resume/read-draft endpoint scoped to the current user only
- [ ] P4.09 Submit endpoint → enters expert queue
  - [ ] P4.09.a Implement submit endpoint setting `SubmittedAt` and status
  - [ ] P4.09.b Trigger `QuestionnaireSubmitted` outbox event
- [ ] P4.10 Expert queue query endpoint with waiting-time calculation
  - [ ] P4.10.a Implement queue listing sorted by waiting time
  - [ ] P4.10.b Compute waiting-time bucket (<24h/24-48h/>48h) server-side for consistent UI rendering
- [ ] P4.11 Guidance authoring + publish endpoint (versioned)
  - [ ] P4.11.a Implement draft-guidance save (expert-only, permission-checked)
  - [ ] P4.11.b Implement publish endpoint creating a new `GuidanceResponse` version
  - [ ] P4.11.c Trigger `GuidancePublished` outbox event
- [ ] P4.12 Bounded follow-up question endpoint
  - [ ] P4.12.a Implement endpoint enforcing the single-follow-up limit
  - [ ] P4.12.b Integration test rejecting a second follow-up attempt
- [ ] P4.13 Notification trigger: `QuestionnaireSubmitted`, `GuidancePublished` via outbox
  - [ ] P4.13.a Wire outbox consumers to `INotificationSender` for both event types
  - [ ] P4.13.b Verify notification content excludes questionnaire/guidance text (§35)

### 4.C Sensitive-data handling (§35)

- [ ] P4.14 Explicit questionnaire consent capture + versioning
  - [ ] P4.14.a Add a consent-capture step before questionnaire start, using `UserConsent` (P1.15)
  - [ ] P4.14.b Version the consent text; require re-consent on version bump
- [ ] P4.15 Restrictive authorization: visible only to submitting client + authorized expert
  - [ ] P4.15.a Implement resource-ownership checks on every read/write endpoint
  - [ ] P4.15.b Integration tests for cross-user and non-expert access denial
- [ ] P4.16 Exclude questionnaire content from logs, analytics, notifications
  - [ ] P4.16.a Audit all log statements in the Questionnaires module for leaked content
  - [ ] P4.16.b Add a lint/code-review checklist item enforcing this going forward
- [ ] P4.17 Audit sensitive reads (`questionnaire.read`)
  - [ ] P4.17.a Emit the audit event on every guidance/submission read by the expert
  - [ ] P4.17.b Verify audit metadata contains no submission content
- [ ] P4.18 Encryption at rest for questionnaire responses and guidance (where feasible)
  - [ ] P4.18.a Evaluate column-level encryption (e.g. via EF Core value converters or PostgreSQL `pgcrypto`)
  - [ ] P4.18.b Implement for `QuestionnaireAnswer.Value` and `GuidanceResponse.Body`
  - [ ] P4.18.c Verify key management approach is documented and not hardcoded
- [ ] P4.19 Self-service export of questionnaire/guidance data
  - [ ] P4.19.a Implement export endpoint producing the user's own submissions + guidance as JSON
  - [ ] P4.19.b Integration test verifying no other user's data leaks into the export
- [ ] P4.20 Deletion workflow for questionnaire data respecting retention policy
  - [ ] P4.20.a Implement deletion/anonymization per the retention policy from P7.06
  - [ ] P4.20.b Integration test verifying deletion doesn't break subscription/account integrity

### 4.D Crisis-related guardrails (§36)

- [ ] P4.21 Localized safety/disclaimer content on psychology-related pages
  - [ ] P4.21.a Draft disclaimer copy (ro/en) with the product/legal-appropriate wording
  - [ ] P4.21.b Wire the disclaimer component onto Psychology-domain screens
- [ ] P4.22 Visible emergency/help information where appropriate
  - [ ] P4.22.a Add an emergency-info component (localized) to relevant screens
  - [ ] P4.22.b Confirm content sourcing/wording with the product owner before launch
- [ ] P4.23 Explicitly confirm no automated clinical-risk classification exists anywhere in the codebase
  - [ ] P4.23.a Code-review pass across Questionnaires and Chat modules for any risk-scoring logic
  - [ ] P4.23.b Document the confirmation in the sensitive-data ADR/strategy doc

### 4.E Client UI

- [ ] P4.24 Questionnaire fill/resume UI
  - [ ] P4.24.a Build the multi-question form with save-as-draft
  - [ ] P4.24.b Wire question-type-specific inputs (Text/LongText/SingleChoice/MultiChoice/Scale)
  - [ ] P4.24.c Wire the consent step before first access
- [ ] P4.25 Guidance reading UI + follow-up submission
  - [ ] P4.25.a Build the guidance-reading view with version history if applicable
  - [ ] P4.25.b Wire the bounded follow-up submission form
- [ ] P4.26 Dashboard "under review" / "guidance available" states (§41)
  - [ ] P4.26.a Build the dashboard card reflecting submission/guidance state
  - [ ] P4.26.b Wire it to the relevant read endpoints

### 4.F Expert/admin UI (§50–51)

- [ ] P4.27 Questionnaire builder UI (question list, reorder, editor, translation switcher, preview, publish)
  - [ ] P4.27.a Build question list + reorder (reuse P2.18 drag-and-drop pattern)
  - [ ] P4.27.b Build question editor with type-specific option/scale config
  - [ ] P4.27.c Wire translation switcher and preview mode
- [ ] P4.28 Submission queue UI with aging indicators (<24h normal, 24–48h attention, >48h overdue)
  - [ ] P4.28.a Build the queue table per §50 columns
  - [ ] P4.28.b Wire the aging-bucket visual treatment from P4.10's server-computed bucket
- [ ] P4.29 Guidance editor: client summary, Q&A cards, timeline, editor, version history, publish action
  - [ ] P4.29.a Build the Q&A card view (not a raw form dump)
  - [ ] P4.29.b Build the guidance rich-text editor + version history panel
  - [ ] P4.29.c Wire publish action with confirmation

### 4.G Tests

- [ ] P4.30 Draft/submit/guidance/versioning lifecycle tests
  - [ ] P4.30.a End-to-end: draft → submit → review → guidance v1 → guidance v2
- [ ] P4.31 Bounded follow-up enforcement test (cannot exceed one)
  - [ ] P4.31.a Second follow-up attempt is rejected
- [ ] P4.32 Cross-user questionnaire access denial tests
  - [ ] P4.32.a User A cannot read/submit against User B's questionnaire submission
- [ ] P4.33 Admin-has-no-implicit-access test
  - [ ] P4.33.a Administrator role without explicit grant cannot read submission/guidance content

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

## Phase 7 — Launch readiness

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

### 7.E Production readiness

- [ ] P7.16 Error monitoring integration
  - [ ] P7.16.a Integrate an error-monitoring provider for backend and frontend
- [ ] P7.17 Production environment configuration (secrets, CORS, rate limits)
  - [ ] P7.17.a Finalize production `.env`/secrets management approach
  - [ ] P7.17.b Lock down CORS to the production frontend origin
  - [ ] P7.17.c Review production rate-limit thresholds
- [ ] P7.18 Backup strategy for PostgreSQL
  - [ ] P7.18.a Configure automated backups and verify a restore drill
- [ ] P7.19 Deployment pipeline
  - [ ] P7.19.a Build the CD pipeline (build → migrate → deploy)
  - [ ] P7.19.b Verify rollback capability
- [ ] P7.20 Full security pass (§65 checklist end-to-end)
  - [ ] P7.20.a Walk the full §65 checklist against the shipped implementation
  - [ ] P7.20.b Remediate any gaps found
- [ ] P7.21 Full audit-log coverage review against §37 action list
  - [ ] P7.21.a Verify every §37-listed action actually emits an audit entry in the shipped code

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
