# B-United Implementation Plan

## 1. Purpose

This document is the execution plan for closing the gaps identified in the 2026-08-09 application audit. It is written so a developer or a fresh coding agent can continue without relying on conversation history.

The plan has two release milestones:

1. **Milestone A — Demo MVP:** complete, stable, reproducible, and suitable for a controlled product demonstration.
2. **Milestone B — Production:** real payment, video, email, storage, deployment, security, backup, and monitoring integrations.

`docs/TASKS.md` remains the detailed backlog and evidence log. This file defines delivery order, technical boundaries, acceptance criteria, and verification requirements.

Current-state note (verified 2026-08-10): the Quiz vertical feature described by `docs/TASKS.md` P2.36-P2.41 is already implemented end to end. Slice A2 below is therefore a regression, live-acceptance, and polish slice, not a net-new implementation slice.

## 2. Required reading before implementation

Read these files before changing production code:

1. `CLAUDE.md`
2. `docs/DEVELOPMENT_INSTRUCTIONS.md`
3. Relevant sections of `docs/PROMPT.md`
4. Relevant ADRs under `docs/adr/`
5. `docs/HANDOVER_PROGRAM_COMMERCE_MIGRATION.md`
6. The implementation and tests currently present in the affected modules
7. `git status --short`

Never infer current behavior only from a handover or unchecked task description. The implementation and executable tests are the source of truth.

## 3. Locked architecture decisions

The following decisions are already accepted and must not be changed incidentally:

- One ASP.NET Core modular monolith, one PostgreSQL database, and one React SPA.
- Single organization and one primary expert. No multi-tenancy or marketplace behavior.
- Clients purchase programs individually through one-time purchases.
- Billing exclusively owns `ProgramOffer`, `ProgramPrice`, `Purchase`, `Payment`, `Invoice`, and `ProgramEntitlement`.
- Program access is checked through `IProgramAccessContext` using both `UserId` and `ProgramId`.
- Cross-module behavior uses Contracts projects. A module must not reference another module's Domain or Infrastructure layer.
- Cross-module identifiers are opaque `Guid` values without database foreign keys across module boundaries.
- Expert, moderator, billing manager, event manager, and administrator permissions remain independent from commercial entitlement.
- Administrators receive no implicit access to questionnaire answers or guidance.
- Published catalogue metadata may remain browseable while protected content is gated by entitlement.
- Legacy fixed chat rooms remain deactivated. Do not fabricate program associations.
- Do not introduce an outbox as part of this plan. Entitlement activation remains synchronous with purchase processing. Reliable external notification delivery will be designed with the selected production provider without introducing speculative infrastructure.
- Romanian is the default UI locale; Romanian and English keys must remain in parity.
- Business-content translations remain in dedicated database translation tables.

Any requested change to these decisions requires an explicit architecture decision and an updated ADR before implementation.

## 4. Engineering rules

### 4.1 Change discipline

- Deliver one buildable vertical slice at a time.
- Preserve unrelated work in the dirty worktree.
- Do not use destructive Git commands.
- Do not commit, push, rewrite history, or delete user data without explicit authorization.
- Define objective, affected modules, permissions, contracts, schema impact, and acceptance criteria before editing.
- Do not add placeholders, unused abstractions, dead code, or fake success paths.
- Update callers, tests, localization, documentation, and configuration in the same slice.

### 4.2 Backend

- Controllers remain thin; business behavior belongs in Application or Domain.
- Use DTOs at API boundaries. Never return EF entities.
- Validate input through the existing FluentValidation pipeline.
- Every database or provider operation must be asynchronous and accept `CancellationToken`.
- Expected failures use stable error codes and message keys.
- Mutations that could leave partial business state require a transaction.
- Concurrency invariants require optimistic concurrency, uniqueness constraints, or atomic database operations.
- Sensitive and security-critical operations produce metadata-only audit records.

### 4.3 Database

- Use EF Core migrations for every schema change.
- Migration names, table names, columns, indexes, and constraints must be English.
- Review every migration for data loss, locking, backfill, nullability, deletion behavior, and rollback implications.
- Every foreign key should be indexed unless a documented query analysis says otherwise.
- Persist money as `decimal` with explicit currency.
- Persist timestamps in UTC and retain display timezone separately where required.
- Verify the complete migration chain against an empty PostgreSQL database.

### 4.4 Frontend

- Use TanStack Query for server state.
- Keep API access in feature API modules and use the shared API client.
- Use React Hook Form and Zod for forms.
- Do not hardcode visible UI text.
- Provide deliberate loading, empty, error, unauthorized, and success states.
- Use design-system tokens and existing primitives.
- Management pages must intentionally adapt to mobile; do not merely shrink desktop tables.
- Meet WCAG 2.2 AA for semantics, keyboard use, focus, contrast, labels, dialogs, tables, and reduced motion.

### 4.5 Security and privacy

- Enforce authentication, permission, ownership, and entitlement on the server.
- Frontend guards are user experience controls, not authorization controls.
- Never trust client-reported amount, currency, purchase success, access state, or quiz score.
- Never log passwords, tokens, questionnaire answers, guidance text, payment-card data, secrets, or raw sensitive payloads.
- Test anonymous, wrong-permission, wrong-owner, wrong-program, revoked-access, and tampered-input paths where applicable.

## 5. Delivery workflow for every slice

Each slice must follow this sequence:

1. Inspect current code, tests, migrations, documentation, and working-tree state.
2. Write the slice objective and acceptance criteria.
3. Identify affected modules and permissions.
4. Define API and database changes before UI implementation.
5. Implement backend behavior and focused tests.
6. Add or review the migration when the schema changes.
7. Implement frontend behavior, responsive states, and translations.
8. Run a security/privacy review proportional to the feature.
9. Run focused tests, then the broadest practical verification suite.
10. Perform live verification against the running API and PostgreSQL when practical.
11. Synchronize `docs/TASKS.md`, relevant ADRs, README, and handover documents.
12. Report changed files, exact verification results, skipped checks, and residual risks.

Do not declare a slice complete if a `BLOCKER` or `HIGH` finding remains.

## 6. Milestone A — Demo MVP

### Slice A0 — Repository and migration stabilization

**Objective:** establish a reproducible baseline before further feature development.

**Affected areas:** repository-wide documentation, Migrations, API startup, frontend environment configuration.

**Tasks:**

- [ ] Inventory all modified and untracked files and group them by completed feature.
- [ ] Review `20260809195723_SyncProgramCommerceModel`; rename or replace it with an accurate quiz-focused migration name if it has not entered a shared environment. If already shared, preserve the migration identifier and document the naming mismatch.
- [ ] Verify that the model snapshot matches the current EF model.
- [ ] Apply the complete migration chain to an empty PostgreSQL database.
- [ ] Verify startup seeders are idempotent.
- [ ] Reconcile local API port documentation with the actual `5080` launch profile and frontend `.env`.
- [ ] Update README startup commands for native PostgreSQL and Docker separately.
- [ ] Update ADR-010 to describe per-program purchases rather than legacy subscriptions.
- [ ] Update ADR-005 references from the removed access stub to `IProgramAccessContext`.
- [ ] Reconcile ADR-008 with the locked no-new-outbox decision.
- [ ] Correct stale checkboxes and evidence in `docs/TASKS.md`.
- [ ] Remove or ignore local runtime artifacts such as API stdout/stderr logs without touching user data.

**Acceptance criteria:**

- A clean database migrates from zero to the current schema.
- API starts without pending-model warnings and `/health/live` returns HTTP 200.
- A fresh developer can start the application using only README.
- Documentation does not describe conflicting entitlement or billing models.
- All builds and tests pass from the stabilized worktree.

### Slice A1 — Permission-aware administration shell

**Objective:** preserve independent administrative permissions in the frontend.

**Affected files/modules:**

- `frontend/src/app/router.tsx`
- `frontend/src/layouts/AdminLayout.tsx`
- `frontend/src/layouts/ClientLayout.tsx`
- `frontend/src/layouts/navigation.ts`
- `frontend/src/shared/auth/RequirePermission.tsx`
- `frontend/src/shared/permissions/`
- layout and router tests

**Technical design:**

- Add a reusable `RequireAnyPermission` route guard or extend the established permission guard without duplicating permission strings.
- The `/admin` shell may open when the user holds at least one supported administrative permission.
- Protect each route group with its exact permission:
  - Content management: `content.create`, `content.edit`, or `content.publish` as applicable.
  - Questionnaire review: `questionnaire.review` / `questionnaire.answer`.
  - Billing: `billing.manage`.
  - Events: `events.manage`.
  - Chat moderation: `chat.moderate`.
  - Users: `users.manage`.
  - Audit: `audit.view`.
- Filter sidebar destinations using the same canonical permission map.
- Show the client-to-admin switch to any user with at least one administrative permission.
- Backend authorization policies remain the final enforcement layer.

**Required tests:**

- [ ] Client cannot open the admin shell.
- [ ] Moderator can open Community moderation without `content.create`.
- [ ] Billing manager can open Billing but cannot open Programs.
- [ ] Event manager can open Events without program-management access.
- [ ] Expert can open the questionnaire review queue without buyer entitlement.
- [ ] Hidden navigation links do not replace route-level guards.

**Acceptance criteria:** role-specialized accounts reach exactly their permitted sections and receive a deliberate forbidden state everywhere else.

### Slice A2 — Quiz regression, acceptance, and polish

**Status:** the core vertical feature is already implemented and recorded as complete in `docs/TASKS.md` P2.36-P2.41.

**Verified existing baseline:**

- `ContentItemType.Quiz`, quiz questions/options/translations, and append-only attempts exist.
- Admin endpoints and `QuizBuilder` support create, translate, reorder, and delete operations.
- `ProgramPlayerPage` renders accessible single-select questions, submits answers, displays per-question feedback and score, permits retakes, and marks content complete.
- Grading is server-side and does not accept a client-provided score.
- Client DTOs do not expose `IsCorrect` before submission.
- `QuizFlowTests` covers correct-answer constraints, reorder-set mismatch, server grading, cross-program denial, option tampering, hidden `IsCorrect`, and append-only retakes.

**Objective:** verify the completed slice as part of the stabilized application and close only confirmed UX, accessibility, or regression gaps.

**Affected modules:** Content tests and API contracts, admin program editor, program player, locale files, and browser E2E coverage. Schema work belongs to A0 only for the misnamed migration; do not redesign the quiz model in this slice.

**QA and polish tasks:**

- [ ] Re-run all `QuizFlowTests` against the stabilized model and migration chain.
- [ ] Live-verify authoring a bilingual quiz, refreshing the editor, reordering questions/options, and deleting/recreating the correct answer.
- [ ] Live-verify entitled completion, incorrect/correct feedback, retake, persisted attempts, and progress completion.
- [ ] Verify denial after entitlement revocation and denial when the user owns a different program.
- [ ] Add frontend regression tests for quiz rendering, submit-disabled-until-complete behavior, result feedback, retake, and API error states if these are not already automated.
- [ ] Add browser E2E coverage for the admin-authoring and client-completion path.
- [ ] Verify keyboard navigation, fieldset/legend semantics, focus after submission, screen-reader feedback, mobile layout, and 200% zoom.
- [ ] Verify Romanian/English content translation and default-language fallback.
- [ ] Confirm publication behavior for an empty or structurally incomplete quiz; add validation only if a reproducible gap exists.
- [ ] Evaluate the documented delete-and-recreate workflow for changing the correct answer. Improve it only if it materially harms the authoring journey; do not add a speculative abstraction.

**Acceptance criteria:** the existing quiz vertical passes backend regression tests, frontend tests, browser E2E, bilingual live verification, revoked/cross-program access checks, and accessibility review, with no client-side leakage of correct answers before grading.

### Slice A3 — Client and role administration

**Objective:** replace the Subscribers placeholder with real client administration.

**Affected modules:** Identity Contracts/Application/Api/Infrastructure/Tests, Admin read models where cross-module summaries are required, Audit, frontend admin users feature, navigation and locales.

**Technical design:**

- Rename the destination from Subscribers to Clients because the product no longer sells subscriptions.
- Add a paginated client list with search and role filters.
- Provide a detail page with identity metadata, roles, purchases, and entitlement summaries.
- Use a dedicated read-only Admin projection for cross-module summaries.
- Never include questionnaire answers or guidance in client administration DTOs.
- Add role assignment and removal through Identity-owned commands.
- Prevent removal of the last effective administrator.
- Audit role changes using `user.role_changed` with metadata only.
- Seed stable Demo accounts for Administrator, Expert, Moderator, and Client roles.

**Required tests:** wrong permission, invalid role, concurrent role changes, last-administrator protection, audit entry, and absence of sensitive questionnaire data.

### Slice A4 — Audit, notifications, and settings navigation

**Objective:** remove remaining visible placeholder destinations.

**Audit:**

- [ ] Add a permission-protected, paginated audit endpoint and screen.
- [ ] Filter by action, actor, entity, and UTC date range.
- [ ] Expose allow-listed metadata only.
- [ ] Test that sensitive questionnaire, token, and webhook payload values cannot appear.

**Notifications:**

- [ ] Decide whether the Demo milestone needs an operational notification history.
- [ ] If it has a current consumer, model status and metadata without storing sensitive bodies.
- [ ] If it has no current consumer, remove the navigation destination instead of leaving a placeholder.

**Settings:**

- [ ] Keep only settings backed by real persisted behavior.
- [ ] Remove the Settings destination if no administration-level settings exist.
- [ ] Do not build a generic settings framework.

**Acceptance criteria:** every visible admin navigation destination leads to working behavior; no `ComingSoonPage` remains in normal navigation.

### Slice A5 — Commercial history and billing UX

**Objective:** make purchase history durable and understandable even when catalogue content changes.

**Backend tasks:**

- [ ] Define immutable commercial snapshot fields on `Purchase` for the program title/label needed in historical records.
- [ ] Backfill existing demo purchases safely.
- [ ] Keep amount and currency server-owned and immutable.
- [ ] Add a client invoice/receipt detail DTO and endpoint if it adds information beyond the current list.
- [ ] Preserve all purchase, payment, invoice, and entitlement history after refund or account deletion according to the retention policy.

**Frontend tasks:**

- [ ] Use snapshot labels for historical purchases instead of querying only the published catalogue.
- [ ] Localize every visible billing and invoice status.
- [ ] Add pagination, filtering, and sorting to client/admin histories where data volume requires it.
- [ ] Keep demo payment controls collapsed and clearly marked.
- [ ] Ensure demo controls are never rendered in Production.

**Required tests:** archived program history, refund, chargeback, duplicate provider event, concurrent provider event, retry after transient failure, and cross-user isolation.

### Slice A6 — Deterministic demo seed and reset

**Objective:** make the full demo journey reproducible without manual database editing.

**Seed data:**

- Administrator, Expert, Moderator, unentitled Client, and entitled Client accounts.
- One complete Romanian/English program.
- Rich text, YouTube video, and quiz content.
- Active offer and representative purchase states.
- Program questionnaire, one submitted response, and guidance state.
- Program-scoped event and chat room.
- Representative moderation report.

**Reset requirements:**

- Add an explicit reset command or endpoint only if it can be safely gated to the `Demo` environment.
- Refuse to run in Production.
- Validate the target database as an explicitly disposable Demo database.
- Never derive a destructive target from a broad or unresolved path/configuration.
- Document recoverability and data loss clearly.

**Acceptance criteria:** reset, start, and both main journeys can be repeated with deterministic credentials and identifiers.

### Slice A7 — UX, responsive behavior, accessibility, and performance

**Objective:** complete a systematic presentation-quality pass.

**Required viewports and modes:**

- 320px mobile width
- Tablet breakpoint
- Desktop
- 200% browser zoom
- Keyboard-only navigation
- Screen-reader semantics review
- Reduced-motion preference

**Known checks:**

- [ ] Fix the `YouTubePlayer` effect cleanup lint warning.
- [ ] Verify player navigation, completion, and failure fallback.
- [ ] Remove technical UUIDs and untranslated enum values from normal UI.
- [ ] Verify fixed sidebars and internal menu scrolling.
- [ ] Verify management forms and tables on mobile.
- [ ] Add accessible labels and error associations to every form.
- [ ] Verify dialogs trap and restore focus.
- [ ] Add route-level lazy loading and reduce the main JavaScript bundle below the current warning threshold where practical.
- [ ] Run an automated accessibility check and manually verify critical journeys.

**Acceptance criteria:** no critical WCAG issue, no horizontal overflow in supported layouts, no inaccessible action, and no unexplained Vite bundle warning for an easily separable route group.

### Slice A8 — End-to-end acceptance and documentation closure

**Client journey:** login, browse, purchase, access, video resume, rich-text completion, quiz, questionnaire, guidance, community, event registration, billing history.

**Admin journey:** program authoring, translations, quiz, publication, offer, questionnaire, guidance, event, moderation, client roles, audit.

**Required automated coverage:**

- [ ] Browser E2E for both journeys.
- [ ] Cross-program buy-A/deny-B coverage.
- [ ] Refund/revoke coverage with historical preservation.
- [ ] Permission-matrix coverage for specialized admin roles.
- [ ] Locale smoke coverage for Romanian and English.
- [ ] Clean-database migration and startup coverage.

**Milestone A exit gate:**

- No visible placeholder navigation.
- No known `BLOCKER` or `HIGH` issue.
- All builds, tests, lint, locale parity, E2E, and migration checks pass.
- Demo reset and startup are documented and reproducible.
- All accepted gaps are documented accurately.

## 7. Milestone B — Production readiness

Milestone B must not begin by weakening the Production safety gate. Replace demo adapters with verified real adapters first.

### Slice B1 — Production session security

- Move refresh-token persistence from JavaScript-readable storage to a Secure, HttpOnly, SameSite cookie design.
- Add an intentional CSRF defense compatible with the chosen cookie strategy.
- Preserve refresh-token hashing, rotation, revocation, and replay detection.
- Add HSTS and security headers.
- Configure explicit production `AllowedHosts` and CORS origins.
- Test XSS/session theft, CSRF, rotation, replay, logout, and revoke-all behavior.

### Slice B2 — Production payment provider

- Select the provider and update ADR-010.
- Implement the provider behind `IPaymentProvider`.
- Use provider-hosted checkout.
- Verify real webhook signatures before processing.
- Preserve provider-event idempotency, concurrency, retry, and out-of-order guarantees.
- Add provider invoice/receipt links and billing portal behavior where supported.
- Run sandbox tests and a controlled production smoke test.
- Never allow a success redirect to grant access.

### Slice B3 — Protected video provider

- Select Mux, Cloudflare Stream, or an equivalent provider and update ADR-005.
- Implement upload initiation, processing-status synchronization, failure retry, duration, and thumbnail metadata.
- Issue short-lived signed playback authorization only after current program-access validation.
- Ensure revoked access cannot obtain a new playback token.
- Plan migration of existing YouTube content without exposing protected assets.

### Slice B4 — Transactional email and notifications

- Select a provider and replace logging senders.
- Add localized templates, delivery status, retry, bounce, and complaint handling.
- Respect user notification preferences.
- Do not include sensitive questionnaire or guidance content in notifications.
- Add provider contract and integration tests.

### Slice B5 — Object storage

- Select storage and implement authorized upload/download.
- Validate size, type, extension, storage key, and provider response.
- Use signed short-lived downloads where required.
- Define retention, lifecycle, deletion, and backup behavior.
- Add malware scanning if allowed file types create that risk.

### Slice B6 — Deployment and operations

- Create reproducible staging and production deployments.
- Add protected secret management and startup validation.
- Define forward migration and rollback procedures.
- Configure PostgreSQL backups and complete a restore drill.
- Add error monitoring with sensitive-data filtering.
- Add operational metrics and alerts for API health, jobs, email, payments, and database readiness.
- Run load tests with representative data.
- Complete security, privacy, accessibility, and performance acceptance reviews.

**Milestone B exit gate:**

- Production starts with no `IDemoOnlyAdapter` registrations.
- Real money activates access only through a validated provider webhook.
- Protected video uses expiring authorization.
- Email delivery and failures are observable.
- Backup restore, deployment, migration, health verification, and rollback have been exercised.
- No known `BLOCKER` or `HIGH` security, privacy, accessibility, or operational issue remains.

## 8. Verification commands

Run focused tests first. Before closing any slice, run the broadest applicable set.

### Backend

```powershell
dotnet restore BUnited.sln
dotnet build BUnited.sln --no-restore --configuration Release
dotnet test BUnited.sln --no-build --configuration Release
```

### Frontend

```powershell
Set-Location frontend
npm.cmd ci
npm.cmd run lint
npm.cmd run check:locale-parity
npm.cmd test -- --run
npm.cmd run build
```

### Local API

```powershell
dotnet run --project src/Api/BUnited.Api.csproj --launch-profile http
```

Expected local address:

```text
http://localhost:5080
```

Health verification:

```powershell
Invoke-WebRequest -Uri http://localhost:5080/health/live -UseBasicParsing
Invoke-WebRequest -Uri http://localhost:5080/health/ready -UseBasicParsing
```

Restart the API after backend changes; the normal `dotnet run` command does not provide application hot reload.

## 9. Slice handoff template

Every completed slice must report:

```text
Outcome:
Affected modules:
API changes:
Schema/migration changes:
Permission and ownership behavior:
Frontend routes/screens:
Localization changes:
Security/privacy review:
Commands executed and exact results:
Live verification performed:
Unverified behavior:
Residual risks:
Documentation updated:
```

## 10. Recommended immediate sequence

Execute the next work in this order:

1. A0 — stabilize migrations, documentation, ports, and worktree understanding.
2. A1 — repair the frontend permission hierarchy.
3. A2 — run quiz regression, live acceptance, accessibility, and polish; do not rebuild the already-complete feature.
4. A3 — implement client and role administration.
5. A4 — remove remaining admin placeholders.
6. A5 — make billing history durable and scalable.
7. A6 — create deterministic Demo seed/reset.
8. A7 — complete responsive, accessibility, and performance passes.
9. A8 — close Demo with browser E2E and synchronized documentation.
10. Begin Milestone B only after Milestone A passes its exit gate.
