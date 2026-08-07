# B-United — Task Backlog

Derived from [`PROMPT.md`](PROMPT.md). Tasks are grouped by delivery phase
(per PROMPT.md §69) plus a Phase 0 for the mandatory pre-implementation
architecture work (§74–75). Each task is numbered `P<phase>.<n>` for stable
referencing in commits, branches and PRs.

Checkbox state reflects implementation status — update as work lands.
Do not reorder task numbers once referenced elsewhere (add new tasks at the
end of their category instead).

---

## Phase 0 — Architecture & Review (before any production code)

### 0.A Architecture deliverables (§74)

- [ ] P0.01 Executive architecture overview
- [ ] P0.02 Module map + module ownership table
- [ ] P0.03 Allowed cross-module dependency map
- [ ] P0.04 Domain event map (outbox events, §13)
- [ ] P0.05 Full database schema (all modules)
- [ ] P0.06 ER relationship overview diagram
- [ ] P0.07 Key index list per table
- [ ] P0.08 Subscription state machine diagram (§16)
- [ ] P0.09 Entitlement flow diagram (§15, §17)
- [ ] P0.10 Payment webhook sequence diagram (§17)
- [ ] P0.11 Authentication / token lifecycle diagram (§65)
- [ ] P0.12 Permission matrix (roles × permissions, §14)
- [ ] P0.13 Questionnaire lifecycle diagram (§26)
- [ ] P0.14 Progress calculation rules writeup (§23–24)
- [ ] P0.15 Event registration / waitlist state machine (§30)
- [ ] P0.16 Localization architecture writeup (§5–6)
- [ ] P0.17 Sensitive-data strategy writeup (§35–36)
- [ ] P0.18 Audit strategy writeup (§37)
- [ ] P0.19 Frontend route map
- [ ] P0.20 Client UI information architecture
- [ ] P0.21 Expert/admin UI information architecture
- [ ] P0.22 Design-system token reference (§56)
- [ ] P0.23 Responsive rules reference (§58)
- [ ] P0.24 API contract overview (§60–61)
- [ ] P0.25 Background-job catalogue (Hangfire)
- [ ] P0.26 Testing strategy document (§68)
- [ ] P0.27 Deployment architecture
- [ ] P0.28 Phase-by-phase backlog (this file)
- [ ] P0.29 Architectural risks and trade-offs register

### 0.B Architecture review (§75)

- [ ] P0.30 Challenge the proposed architecture: flag anything unnecessary, overengineered, underspecified, too tightly coupled, too generic, delay-prone or maintenance-risky
- [ ] P0.31 For each issue found: document Issue / Why it matters / Recommended change / Trade-off
- [ ] P0.32 Get explicit approval on the (possibly revised) architecture before Phase 1 implementation starts

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

---

## Phase 1 — Foundation

Deliverable: production-shaped skeleton where users can register, verify email, log in and navigate localized UI.

### 1.A Solution & infrastructure

- [ ] P1.01 Initialize .NET solution and module projects per §11 structure
- [ ] P1.02 Initialize Vite + React + TypeScript app per §39 structure
- [ ] P1.03 PostgreSQL via Docker Compose, connection string configuration
- [ ] P1.04 EF Core base `DbContext` + per-module configuration convention (BuildingBlocks/Infrastructure)
- [ ] P1.05 Serilog structured logging setup (BuildingBlocks/Observability)
- [ ] P1.06 Health checks endpoint
- [ ] P1.07 OpenAPI / Swagger setup
- [ ] P1.08 ASP.NET rate limiting middleware
- [ ] P1.09 Standardized error-response middleware (§61 contract)
- [ ] P1.10 CI foundation (build, test, lint on push)
- [ ] P1.11 Base Dockerfile(s) for the Api host

### 1.B Identity module (§14)

- [ ] P1.12 `User`, `Role`, `Permission`, `RolePermission`, `UserRole` entities + EF configuration
- [ ] P1.13 `RefreshToken` entity: hashed, rotating, revocable (§65)
- [ ] P1.14 `EmailVerificationToken`, `PasswordResetToken` entities
- [ ] P1.15 `UserConsent`, `UserPreference` entities
- [ ] P1.16 Seed initial roles: `Client`, `Expert`, `Administrator`
- [ ] P1.17 Seed initial permission set (content.*, questionnaire.*, events.*, chat.*, billing.*, users.manage, audit.view)
- [ ] P1.18 Registration endpoint + password hashing
- [ ] P1.19 Email verification flow
- [ ] P1.20 Login endpoint issuing JWT access token + rotating refresh token
- [ ] P1.21 Refresh-token rotation + revocation endpoint
- [ ] P1.22 Password reset flow
- [ ] P1.23 Permission-based authorization policies (no `if (user.Role == "Expert")` in controllers)
- [ ] P1.24 Account lockout / abuse protection on auth endpoints

### 1.C Localization infrastructure

- [ ] P1.25 i18next + react-i18next setup with lazy-loaded namespaces
- [ ] P1.26 Seed `ro`/`en` locale namespace files (common, auth) with real keys
- [ ] P1.27 Language switcher component
- [ ] P1.28 DB-backed translation lookup infrastructure (BuildingBlocks/Localization) with default-language fallback + `translationFallbackUsed` flag pattern (used from Phase 2 onward)

### 1.D Design system foundation

- [ ] P1.29 Design tokens (color, spacing, typography, radius, shadow, breakpoints, focus ring, motion) per §56
- [ ] P1.30 Core primitives: Button, Input, Card, Badge, Alert, Toast, Skeleton, EmptyState
- [ ] P1.31 Base layouts: client layout shell, expert/admin layout shell

### 1.E Audit foundation

- [ ] P1.32 `AuditLog` entity + write API (BuildingBlocks or Audit module)
- [ ] P1.33 Wire audit events: `user.login`, `user.failed_login`, `user.password_reset`, `user.role_changed`

### 1.F Tests

- [ ] P1.34 Auth flow tests (register, verify, login, refresh, reset)
- [ ] P1.35 Permission policy enforcement tests (positive + negative)

---

## Phase 2 — Content

Deliverable: the expert can publish programs and clients can consume them.

### 2.A Domain & schema

- [ ] P2.01 `Domain` entity, seed 5 initial domains (Psychology, Sport, Nutrition, Business, FinancialEducation)
- [ ] P2.02 `Program` + `ProgramTranslation` entities (§19)
- [ ] P2.03 `Section` + `SectionTranslation` entities (§20)
- [ ] P2.04 `ContentItem` + `ContentItemTranslation` entities, types `Video`/`RichText` (§21)
- [ ] P2.05 `MediaAsset` entity + processing-status enum (§22)
- [ ] P2.06 Migrations for all Content tables with FK indexes

### 2.B Video provider integration

- [ ] P2.07 Video-provider abstraction interface (Mux / Cloudflare Stream / Vimeo)
- [ ] P2.08 Upload flow → provider → `MediaAsset` metadata sync
- [ ] P2.09 Signed/short-lived playback URL issuance gated on active `PlatformAccess` (stub `IAccessContext` until Phase 3 lands)

### 2.C Backend API

- [ ] P2.10 Program/Section/ContentItem CRUD endpoints (expert-only, `content.*` permissions)
- [ ] P2.11 Publish/unpublish/archive workflow endpoints
- [ ] P2.12 Client-facing read endpoints with translation fallback applied
- [ ] P2.13 Content ordering/reorder endpoints

### 2.D Admin authoring UI

- [ ] P2.14 Program list screen (All/Drafts/Published/Archived) per §47
- [ ] P2.15 Three-area program editor (Structure / Editor / Properties) per §48
- [ ] P2.16 Rich text editor component
- [ ] P2.17 Video configuration UI (upload trigger, processing status)
- [ ] P2.18 Drag-and-drop reordering for sections/content items
- [ ] P2.19 Contextual translation status UI (Complete / Missing X) per §49

### 2.E Client UI

- [ ] P2.20 Programs screen: domain filter, program cards, CTA state (Start/Continue/Completed) per §42
- [ ] P2.21 Program detail screen per §43
- [ ] P2.22 Program player: desktop 3-pane layout, mobile curriculum drawer per §44
- [ ] P2.23 Video player component with resume position

### 2.F Progress tracking (§23–24)

- [ ] P2.24 `ContentProgress` entity + status enum
- [ ] P2.25 `SectionProgress` entity (denormalized for dashboards)
- [ ] P2.26 Video auto-complete at ~90% watched + periodic (~15s) position persistence, plus pause/navigate/close/complete triggers
- [ ] P2.27 Rich-text manual "Mark as completed" action
- [ ] P2.28 Derived program-progress calculation (no persisted `ProgramProgress` table unless proven necessary)

### 2.G Localization content

- [ ] P2.29 `ro`/`en` UI locale entries for `content.json`
- [ ] P2.30 Seed at least one fully translated demo program (ro + en) for manual verification

### 2.H Tests

- [ ] P2.31 Translation fallback tests (missing translation → default + flag)
- [ ] P2.32 Video completion threshold tests
- [ ] P2.33 Video resume-position tests
- [ ] P2.34 Rich-text manual completion tests
- [ ] P2.35 Playback URL authorization tests (denied without access)

---

## Phase 3 — Billing and access

Deliverable: only valid subscribers can access protected platform functionality.

### 3.A Schema (§15)

- [ ] P3.01 `Plan`, `PlanPrice` entities (decimal + explicit currency, §63)
- [ ] P3.02 `Subscription`, `SubscriptionPeriod` entities + state enum (§16)
- [ ] P3.03 `PaymentCustomer`, `Payment`, `Invoice` entities
- [ ] P3.04 `WebhookEvent` entity (raw event storage, unique provider event ID)
- [ ] P3.05 `Entitlement` entity used as `PlatformAccess` in V1

### 3.B Stripe integration (§17)

- [ ] P3.06 Checkout Session creation endpoint
- [ ] P3.07 Webhook endpoint: signature validation
- [ ] P3.08 Webhook idempotent processing keyed on provider event ID
- [ ] P3.09 Out-of-order event handling
- [ ] P3.10 Webhook → Subscription state transition logic
- [ ] P3.11 Subscription → `PlatformAccess` entitlement update
- [ ] P3.12 Structured audit trail for webhook processing (`payment.webhook_processed`)
- [ ] P3.13 Checkout-success redirect treated as informational only (no access granted client-side)

### 3.C Entitlement consumption

- [ ] P3.14 `IAccessContext` contract (`HasPlatformAccessAsync`, `RequirePlatformAccessAsync`)
- [ ] P3.15 Wire `IAccessContext` into Content playback authorization (replace Phase 2 stub)
- [ ] P3.16 Subscription state rules: Trialing/Active allowed, PastDue grace period (default 3 days), Canceled access-until-period-end, Expired no access (§16)

### 3.D Billing portal & UI

- [ ] P3.17 Client billing screen: subscription status, current period, payment state
- [ ] P3.18 Stripe billing portal hand-off
- [ ] P3.19 Invoice list/download

### 3.E Admin billing UI (§54)

- [ ] P3.20 Subscriber table (Subscriber, Email, Status, Current Period, Access Until, Payment State, Created)
- [ ] P3.21 Subscription detail view (plan, provider id, status, period, payments, invoices, entitlement, webhook timeline)
- [ ] P3.22 Restrict raw webhook payload visibility to technical administrators

### 3.F Tests (§68 highest risk area)

- [ ] P3.23 Webhook idempotency tests
- [ ] P3.24 Out-of-order webhook event tests
- [ ] P3.25 Cancellation → access-until-period-end test
- [ ] P3.26 Expiration → access revoked test
- [ ] P3.27 Grace period boundary tests (PastDue)
- [ ] P3.28 Re-subscription restores access test
- [ ] P3.29 Entitlement tests for every subscription state
- [ ] P3.30 Cross-user billing data access denial tests

---

## Phase 4 — Questionnaire and guidance

Deliverable: expert-led personalization works end-to-end.

### 4.A Schema (§25, §27–28)

- [ ] P4.01 `Questionnaire`/`QuestionnaireTranslation` entities
- [ ] P4.02 `Question`/`QuestionTranslation`, `QuestionOption`/`QuestionOptionTranslation` entities, types Text/LongText/SingleChoice/MultiChoice/Scale
- [ ] P4.03 `QuestionnaireSubmission` with operational timestamps (`CreatedAt, StartedAt, SubmittedAt, AssignedAt, ReviewedAt, AnsweredAt`)
- [ ] P4.04 `QuestionnaireAnswer` entity
- [ ] P4.05 `GuidanceResponse` entity with `Version` field (append, never silently overwrite)
- [ ] P4.06 `GuidanceFollowUp` entity (single bounded follow-up, not messaging)

### 4.B Backend workflow (§26)

- [ ] P4.07 Questionnaire builder endpoints (expert)
- [ ] P4.08 Draft save/resume endpoints (client)
- [ ] P4.09 Submit endpoint → enters expert queue
- [ ] P4.10 Expert queue query endpoint with waiting-time calculation
- [ ] P4.11 Guidance authoring + publish endpoint (versioned)
- [ ] P4.12 Bounded follow-up question endpoint
- [ ] P4.13 Notification trigger: `QuestionnaireSubmitted`, `GuidancePublished` via outbox

### 4.C Sensitive-data handling (§35)

- [ ] P4.14 Explicit questionnaire consent capture + versioning
- [ ] P4.15 Restrictive authorization: visible only to submitting client + authorized expert
- [ ] P4.16 Exclude questionnaire content from logs, analytics, notifications
- [ ] P4.17 Audit sensitive reads (`questionnaire.read`)
- [ ] P4.18 Encryption at rest for questionnaire responses and guidance (where feasible)
- [ ] P4.19 Self-service export of questionnaire/guidance data
- [ ] P4.20 Deletion workflow for questionnaire data respecting retention policy

### 4.D Crisis-related guardrails (§36)

- [ ] P4.21 Localized safety/disclaimer content on psychology-related pages
- [ ] P4.22 Visible emergency/help information where appropriate
- [ ] P4.23 Explicitly confirm no automated clinical-risk classification exists anywhere in the codebase

### 4.E Client UI

- [ ] P4.24 Questionnaire fill/resume UI
- [ ] P4.25 Guidance reading UI + follow-up submission
- [ ] P4.26 Dashboard "under review" / "guidance available" states (§41)

### 4.F Expert/admin UI (§50–51)

- [ ] P4.27 Questionnaire builder UI (question list, reorder, editor, translation switcher, preview, publish)
- [ ] P4.28 Submission queue UI with aging indicators (<24h normal, 24–48h attention, >48h overdue)
- [ ] P4.29 Guidance editor: client summary, Q&A cards, timeline, editor, version history, publish action

### 4.G Tests

- [ ] P4.30 Draft/submit/guidance/versioning lifecycle tests
- [ ] P4.31 Bounded follow-up enforcement test (cannot exceed one)
- [ ] P4.32 Cross-user questionnaire access denial tests
- [ ] P4.33 Admin-has-no-implicit-access test

---

## Phase 5 — Events

Deliverable: subscribers can discover and register for live activities.

### 5.A Schema (§29–31)

- [ ] P5.01 `Event`/`EventTranslation` entities (LocationType, Status enums)
- [ ] P5.02 `EventRegistration` entity + state enum (Registered/Waitlisted/Canceled)
- [ ] P5.03 `EventReminder` entity

### 5.B Backend logic

- [ ] P5.04 Event authoring endpoints (expert, translations, timezone-aware)
- [ ] P5.05 Registration endpoint requiring active `PlatformAccess`
- [ ] P5.06 Capacity + waitlist logic; promote oldest waitlisted user on cancellation
- [ ] P5.07 Registration closes at event start
- [ ] P5.08 Hangfire jobs: 24h and 1h reminders — idempotent, retryable, locale-aware, timezone-aware
- [ ] P5.09 Respect notification preferences for reminders
- [ ] P5.10 Outbox events: `EventPublished`, `EventRegistrationCreated`

### 5.C Client UI

- [ ] P5.11 Event listing + detail screens
- [ ] P5.12 Registration/waitlist UI with status feedback
- [ ] P5.13 Dashboard "upcoming event" card (§41)

### 5.D Admin UI (§52)

- [ ] P5.14 Event list (Title, Date, Type, Registrations, Capacity, Status, Actions)
- [ ] P5.15 Event editor (translations, date/time, timezone, location, capacity, publication status, reminders)
- [ ] P5.16 Event detail: registered users, waitlist, attendance, reminders

### 5.E Tests

- [ ] P5.17 Capacity + waitlist promotion tests
- [ ] P5.18 Timezone handling tests (display vs UTC storage)
- [ ] P5.19 Reminder scheduling idempotency tests
- [ ] P5.20 Registration-requires-access tests

---

## Phase 6 — Community (Chat)

May move after launch under delivery pressure (§69).

### 6.A Schema (§33)

- [ ] P6.01 Fixed room definitions (General, Psychology, Sport, Nutrition, Business, FinancialEducation) — no dynamic room creation
- [ ] P6.02 Message entity (soft delete, pin flag)
- [ ] P6.03 Report entity, Mute entity

### 6.B Backend

- [ ] P6.04 SignalR hub (fallback to polling if it becomes a blocker)
- [ ] P6.05 Paginated message history endpoint
- [ ] P6.06 Basic unread-state tracking
- [ ] P6.07 Pin/unpin endpoint (moderation permission)
- [ ] P6.08 Delete-message moderation endpoint (soft delete)
- [ ] P6.09 Temporary mute endpoint
- [ ] P6.10 Report-message endpoint
- [ ] P6.11 Anonymize deleted user's identity in message history (preserve continuity, §66)

### 6.C Client UI

- [ ] P6.12 Room list/switcher
- [ ] P6.13 Message list with pagination + pinned message highlight
- [ ] P6.14 Persistent localized privacy notice per room (§34)
- [ ] P6.15 Report-message action in UI

### 6.D Admin moderation UI (§53)

- [ ] P6.16 Reported Messages screen
- [ ] P6.17 Muted Users screen
- [ ] P6.18 Recent Moderator Actions screen
- [ ] P6.19 Per-report actions: Dismiss, Delete Message, Mute User

### 6.E Tests

- [ ] P6.20 Moderation action tests (delete, mute, pin) with permission checks
- [ ] P6.21 Report flow test
- [ ] P6.22 Anonymization-on-delete test preserving message continuity

---

## Phase 7 — Launch readiness

### 7.A Expert dashboard & admin views (§46, §38)

- [ ] P7.01 Expert dashboard: pending questionnaires, oldest unanswered, upcoming events, active subscribers, recent subscription changes, reported messages, recent published content
- [ ] P7.02 KPI cards: active subscribers, pending questionnaires, upcoming events, monthly subscription revenue
- [ ] P7.03 `SubscriberAdminView` cross-module read-only projection (Identity + Billing + Progress + last activity)

### 7.B GDPR / data rights (§66)

- [ ] P7.04 Self-service data export (JSON archive + owned attachments)
- [ ] P7.05 Deletion workflow: hard delete vs anonymization vs retained billing records
- [ ] P7.06 Documented retention policy

### 7.C Accessibility (§59)

- [ ] P7.07 WCAG 2.2 AA audit pass: keyboard nav, focus states, semantic HTML, labels, contrast
- [ ] P7.08 Accessible dialogs/tables audit
- [ ] P7.09 Video captions/subtitles support
- [ ] P7.10 Reduced-motion preference support

### 7.D Performance (§67)

- [ ] P7.11 Load-test representative scenario (~2,000 subscribers / ~200 concurrent)
- [ ] P7.12 Dashboard query performance pass
- [ ] P7.13 Chat pagination performance check
- [ ] P7.14 CDN video delivery verification
- [ ] P7.15 Index review against real query patterns

### 7.E Production readiness

- [ ] P7.16 Error monitoring integration
- [ ] P7.17 Production environment configuration (secrets, CORS, rate limits)
- [ ] P7.18 Backup strategy for PostgreSQL
- [ ] P7.19 Deployment pipeline
- [ ] P7.20 Full security pass (§65 checklist end-to-end)
- [ ] P7.21 Full audit-log coverage review against §37 action list

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
