# B-United Mandatory Development Instructions

These instructions apply to every architecture, implementation, review, test, migration, configuration, and documentation task in this repository. Treat every `MUST`, `MUST NOT`, `REQUIRED`, and `NEVER` statement as a release gate. Do not waive a rule silently. If rules conflict or a justified exception is necessary, stop before implementation, document the conflict and trade-off, and obtain an explicit decision.

## 1. Scope and architecture

- MUST preserve the V1 boundaries in `docs/PROMPT.md`.
- MUST implement one deployable ASP.NET Core modular monolith, one PostgreSQL database, and one React SPA.
- MUST NOT add multi-tenancy, microservices, Kubernetes, message brokers, distributed caches, marketplaces, generic plugin engines, or speculative future entities.
- MUST deliver one buildable vertical slice at a time.
- MUST keep module ownership explicit and prevent circular dependencies.
- A module MUST NOT reference another module's Domain or Infrastructure layer.
- Cross-module behavior MUST use stable Contracts or an intentional in-process service.
- Cross-module database joins MUST be read-only and limited to dedicated admin/dashboard read models.
- Domain events and the outbox MUST be used only where retry or reliable cross-module delivery has business value.
- Every durable architecture decision MUST be reflected in an ADR. Do not create ADRs for trivial implementation details.

## 2. Change discipline

- MUST inspect the current implementation, relevant tests, working-tree status, prompt sections, backlog items, and ADRs before editing.
- MUST preserve unrelated user changes and work safely in a dirty working tree.
- MUST NOT use destructive Git commands or overwrite user work without explicit authorization.
- MUST define the objective, affected modules, acceptance criteria, permissions, contracts, schema impact, and verification plan before a material implementation.
- MUST prefer the smallest complete change that satisfies the current requirement.
- MUST NOT add placeholders, fake success paths, dead code, unused abstractions, or unresolved TODO architecture.
- MUST update all affected callers, tests, translations, documentation, and configuration in the same change.
- MUST distinguish confirmed findings from assumptions and unverified behavior.

## 3. Language and naming

- All code, identifiers, API routes, database objects, migration names, tests, comments, logs, configuration keys, and technical documentation MUST be in English.
- User-facing text MUST use localization keys. Visible text MUST NOT be hardcoded in React components.
- Names MUST express business intent. Avoid vague names such as `Helper`, `Manager`, `Data`, `Utils`, or `Common` unless the abstraction is genuinely generic and cohesive.
- Permission names, error codes, event names, and localization keys MUST have one canonical definition and MUST NOT be duplicated as arbitrary strings.

## 4. Backend engineering

- Controllers/endpoints MUST remain thin and delegate business behavior to the Application or Domain layer.
- Domain invariants MUST be enforced in domain/application code, not in controllers, EF configurations, or UI components alone.
- API boundaries MUST use request/response DTOs. EF entities MUST NEVER be returned directly.
- Inputs MUST be validated with FluentValidation or an equivalent established project mechanism.
- Database and external-provider operations MUST be asynchronous and accept `CancellationToken`.
- Expected failures MUST use the standardized error contract with stable `code`, `messageKey`, and `correlationId`.
- Exceptions MUST NOT expose stack traces, SQL details, provider payloads, or secrets to clients.
- Mutations that can leave partial business state MUST use an explicit transaction.
- Concurrent mutations MUST use appropriate optimistic concurrency, uniqueness constraints, or atomic database operations.
- External-provider operations and background jobs MUST be idempotent where retries are possible.
- OpenAPI contracts MUST match runtime behavior, authentication, validation, and response codes.

## 5. Database and migrations

- Every important table MUST define a primary key, foreign keys, nullability, deletion behavior, timestamps, and concurrency behavior where relevant.
- Every foreign key MUST be indexed unless a documented query analysis proves the index unnecessary.
- Unique business invariants MUST be protected by database constraints when possible.
- Indexes MUST correspond to real query patterns; do not index every column mechanically.
- Money MUST use `decimal` and store currency explicitly. NEVER use floating-point types for financial values.
- Persist timestamps in UTC. Event records MUST retain their display timezone separately.
- Schema changes MUST use English-named EF Core migrations. Manual production schema drift is forbidden.
- Migrations MUST be reviewed for data loss, locking, defaults, backfill behavior, rollback implications, and clean-database applicability.
- Deletion behavior MUST be explicit. Subscription expiration MUST NEVER delete the account or historical user data.

## 6. Security and privacy

- Treat every client request and identifier as untrusted.
- Authentication, permissions, resource ownership, and entitlement decisions MUST be enforced server-side on every applicable read and mutation.
- Client-side route guards and hidden UI controls MUST NEVER be treated as authorization.
- Billing is the sole owner of subscription and `PlatformAccess` state. Other modules MUST use `IAccessContext`.
- Browser redirects or client-reported payment state MUST NEVER activate access.
- Payment webhooks MUST validate signatures, deduplicate provider event IDs, tolerate retries, and handle out-of-order delivery safely.
- Refresh tokens MUST be hashed, rotating, revocable, and protected against replay.
- Public and authentication endpoints MUST have intentional rate limits and abuse controls.
- CORS MUST use explicit trusted origins in production. Wildcard origins with credentials are forbidden.
- Bind only explicit DTO fields. Prevent mass assignment and over-posting.
- File uploads MUST validate authorization, size, type, extension, storage key, and provider response. Never trust the client filename.
- Video playback URLs/tokens MUST be short-lived and issued only after current access authorization.
- Secrets MUST come from protected configuration and MUST NOT be committed, returned, printed, or logged.
- NEVER log passwords, access tokens, refresh tokens, reset/verification tokens, questionnaire answers, guidance text, payment card data, secrets, or raw sensitive payloads.
- Questionnaire submissions and guidance MUST be accessible only to the submitting client and explicitly authorized expert. Administrators have no implicit access.
- Sensitive reads and security-critical mutations MUST produce metadata-only audit records.
- Data export and deletion MUST enforce identity, authorization, retention, anonymization, and legally required billing retention.

## 7. Frontend engineering

- Server state MUST use TanStack Query. Zustand MAY be used only for minimal application-wide client state.
- Forms MUST use React Hook Form and Zod unless an established component requires another documented approach.
- API access MUST live outside presentation components and use the shared API client.
- Every request MUST handle non-success responses before parsing or consuming success data.
- Components MUST expose deliberate loading, empty, error, unauthorized, and success states.
- Business logic MUST NOT be embedded in presentational React components.
- Reusable primitives MUST use design-system tokens; do not scatter arbitrary colors, spacing, radii, or shadows.
- Prefer composition and feature-local components. Move code to `shared` only after it has a stable cross-feature purpose.
- Management tables MUST adapt intentionally on mobile using cards, condensed rows, drawers, or context menus.
- Interactive targets SHOULD be at least 44px where practical.
- MUST meet WCAG 2.2 AA: semantic HTML, labels, keyboard operation, visible focus, sufficient contrast, accessible dialogs/tables, reduced motion, and captions where applicable.

## 8. Localization

- Romanian is the default UI locale and English is required from launch.
- `ro` and `en` locale files MUST maintain key parity in the same change.
- UI translations MUST remain in source-controlled i18next namespaces.
- Programs, sections, content items, events, questions, and options MUST use dedicated database translation tables.
- Missing business-content translations MUST fall back to the entity's default language.
- Administrative DTOs and screens MUST expose and visibly represent translation fallback/missing status.
- Formatting MUST use the active locale and user timezone; do not hand-build localized dates, times, or money strings.

## 9. Testing

- Every behavior change MUST include the smallest effective automated regression test unless testing is technically impossible and explicitly reported.
- Tests MUST verify business outcomes, not only HTTP status codes or mocked method calls.
- Protected endpoints MUST include anonymous, wrong-permission, wrong-owner, expired-access, and tampered-input cases where applicable.
- Critical state transitions MUST include boundary, retry, idempotency, ordering, and concurrency tests.
- Database behavior MUST be covered by integration tests against PostgreSQL where provider-specific behavior matters.
- Billing MUST test webhook duplication, out-of-order events, grace periods, cancellation, expiration, and re-subscription.
- Questionnaire tests MUST prove cross-user isolation and the absence of implicit administrator access.
- Localization tests MUST cover lookup, fallback, and Romanian/English key parity.
- Event tests MUST cover capacity, waitlist promotion, cancellation, reminders, and timezone boundaries.
- Progress tests MUST cover resume, periodic updates, the video completion threshold, and manual rich-text completion.
- MUST run focused tests first and the broadest practical build/test/lint suite before declaring completion.
- MUST report commands executed, results, skipped checks, and residual risks truthfully.

## 10. Observability and operations

- Logs MUST be structured and use stable event names with correlation identifiers.
- Log only the minimum metadata required to operate and investigate the system.
- Health checks MUST distinguish application liveness from dependency readiness where deployment needs both.
- Background jobs MUST be observable, retry-safe, idempotent, and cancellation-aware where supported.
- Production configuration MUST fail safely when required secrets are absent or placeholder values remain.
- Docker and CI builds MUST be reproducible from a clean checkout.
- Deployment changes MUST document migrations, configuration, health verification, backup impact, and rollback strategy.

## 11. Completion gate

A task is complete only when:

- the requested behavior works end to end;
- applicable architecture, security, privacy, database, API, frontend, localization, and accessibility rules pass;
- builds, tests, linting, and migrations pass at the broadest practical level;
- documentation, ADRs, OpenAPI, locale resources, and `docs/TASKS.md` are synchronized;
- no known `BLOCKER` or `HIGH` issue remains;
- changed files, verification evidence, unverified items, and remaining risks are reported.

If any applicable condition is unmet, report the task as incomplete. Never claim completion based only on code generation or a successful typecheck.

