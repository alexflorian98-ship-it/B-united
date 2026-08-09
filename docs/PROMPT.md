# Prompt — Personal Development Platform V1 — Refined Modular Architecture

This is the authoritative product/architecture specification for B-United.
It is reproduced verbatim from the original architecture prompt so the whole
team works from the same source of truth. See `docs/adr/` for the resulting
Architecture Decision Records and `README.md` for the current repository
structure.

---

You are a senior software architect, senior full-stack engineer, database architect, security engineer and product UX designer.

Design and implement a production-ready **single-organization personal development web platform**.

This is **Version 1**.

The goal is to build a commercially viable SaaS-like product with strong engineering foundations, without introducing enterprise complexity that is not required by the current business model.

Scope discipline is mandatory.

Do not build speculative architecture for future marketplace, multi-tenancy or microservices.

The system should be easy to maintain, easy to extend and realistic for a small development team or a single experienced developer.

---

## 1. Product context

The application is owned by one expert / business owner.

The owner sells structured personal development programs individually through one-time purchases.

Clients may browse the program catalogue before purchasing. A completed purchase grants permanent access only to the purchased program and to the questionnaires, guidance, progress, community rooms and events explicitly associated with that program.

The platform contains the following initial domains:

* Psychology
* Sport
* Nutrition
* Business
* Financial Education

The architecture must allow additional domains later without requiring structural redesign.

However, different domains must NOT introduce separate domain-specific engines in V1.

All five domains use the same generic content system.

---

## 2. Explicit V1 boundaries

V1 is: single organization; one primary expert; program-purchase-based; responsive web; multilingual; content-oriented; questionnaire-driven; progress-aware; community-enabled; event-enabled.

V1 is NOT: a marketplace; a multi-tenant platform; a multi-organization platform; a coach marketplace; a native mobile application; a medical platform; a financial advisory platform; a social network; a learning management system for enterprises.

Do not design unnecessary abstractions for these scenarios. Leave reasonable extension points only where they cost very little.

---

## 3. Core business rules

1. Users register and maintain their account independently of purchase or entitlement state.
2. Authenticated users may browse the published program catalogue and commercial program details without purchasing.
3. Each program is purchased separately through a one-time payment.
4. A confirmed purchase grants permanent access only to the purchased program and its explicitly associated functionality.
5. Purchasing one program never grants access to another program.
6. Permanent access has no normal expiration date, but may be revoked for a refund, chargeback, fraud or an audited administrative correction.
7. Revoking access must NOT delete: the user account; purchase and invoice history; questionnaire submissions; expert guidance; progress; event history; chat history; preferences.
8. Access decisions must always be enforced server-side using both `UserId` and `ProgramId`.
9. Program content is the same for all clients entitled to that program.
10. Personalization happens through written expert guidance, not dynamically generated program variants.
11. Group discussion exists only in predefined rooms associated with one or more programs.
12. The platform must support Romanian and English from launch.
13. All technical implementation must be written exclusively in English.

---

## 4. Technical language rules

Everything technical must use English: source code, database schema, table names, column names, classes, interfaces, enums, methods, variables, API routes, DTOs, validators, logs, tests, configuration, comments, documentation, migration names, Git branch suggestions, commit messages.

No Romanian identifiers are allowed in technical implementation.

---

## 5. UI localization

User-facing UI must support multiple languages. Initial languages: Romanian (default), English.

Use: i18next; react-i18next; lazy-loaded namespaces. Do not hardcode visible UI text.

```ts
t("dashboard.continueProgram")
t("subscription.status.active")
t("events.register")
```

UI translations live in source-controlled locale files:

```text
src/locales/
  ro/
    common.json
    auth.json
    dashboard.json
    content.json
    billing.json
    questionnaire.json
    events.json
    chat.json
  en/
    ...
```

Adding another UI language should require only translation resources, not component changes.

---

## 6. Content localization

UI localization and business-content localization are separate concerns.

Programs, sections, events and questionnaire content must support translation through dedicated database translation tables:

```text
Program / ProgramTranslation
Section / SectionTranslation
ContentItem / ContentItemTranslation
Event / EventTranslation
Question / QuestionTranslation
QuestionOption / QuestionOptionTranslation
```

Every translatable entity should have a default language. If a requested translation is unavailable: (1) return the default-language version; (2) expose a `translationFallbackUsed` flag in administrative DTOs; (3) visibly mark missing translations in admin screens.

Do NOT implement a generic database-driven localization system for ordinary UI labels in V1. UI labels belong in frontend locale files.

---

## 7. Default technology stack — Frontend

React, TypeScript, Vite, React Router, TanStack Query, React Hook Form, Zod, Zustand (minimal app-wide client state only), Tailwind CSS, i18next, react-i18next, SignalR client where required, accessible reusable UI components.

Do not use global state for server data that belongs in TanStack Query.

---

## 8. Backend

ASP.NET Core Web API, C#, Entity Framework Core, FluentValidation, PostgreSQL, JWT access tokens, hashed rotating refresh tokens, policy/permission-based authorization, Hangfire, Serilog, health checks, OpenAPI/Swagger, ASP.NET rate limiting, structured error responses.

CQRS/MediatR may be used when it improves separation, but do not force a command/handler abstraction around trivial operations. Avoid ceremony without business value.

---

## 9. Infrastructure

PostgreSQL; Docker; Docker Compose; migrations; object storage abstraction; transactional email abstraction; payment abstraction; video-provider abstraction; production-ready environment configuration.

Use a dedicated video service (Mux, Cloudflare Stream or Vimeo). Do not serve uploaded video from the application server. The application database stores only video metadata and provider identifiers. Playback URLs must be short-lived and issued only after successful access authorization.

---

## 10. Architectural style

Modular monolith. One deployable ASP.NET application. One PostgreSQL database. Clear module ownership.

Do NOT use: microservices; Kubernetes; distributed cache; Kafka; RabbitMQ; service mesh; database-per-module; sharding; distributed transactions. They are unnecessary for V1.

---

## 11. Backend solution structure

```text
src/
  BuildingBlocks/
    Application/
    Domain/
    Infrastructure/
    Security/
    Localization/
    Observability/
  Modules/
    Identity/       (Domain, Application, Infrastructure, Api, Contracts, Tests)
    Content/
    Progress/
    Questionnaires/
    Billing/
    Notifications/
    Events/
    Chat/
    Files/
    Audit/
    Admin/
  Api/
  Jobs/
  Migrations/
```

---

## 12. Practical module boundaries

* one module must not directly manipulate another module's domain entities;
* one module must not directly depend on another module's Infrastructure layer;
* cross-module commands should go through contracts/services;
* domain events should be used for asynchronous business reactions where they add value;
* no circular module dependencies.

**Do not prohibit every cross-module database query.** Read-only administrative and dashboard projections may join data across module-owned tables when this materially simplifies the application (admin dashboard, customer purchase overview, operational reports, billing + user overview).

These queries must: remain read-only; live in dedicated query/read-model code; never mutate another module's data; not become hidden business dependencies. Avoid building artificial event projections merely to display a dashboard.

---

## 13. Transactional outbox

Use a transactional outbox for important cross-module events where failure or retry matters: `ProgramPurchased`, `ProgramAccessRevoked`, `PaymentFailed`, `QuestionnaireSubmitted`, `GuidancePublished`, `EventPublished`, `EventRegistrationCreated`.

Good use: a validated payment event causes Billing to create a `ProgramEntitlement` and emit `ProgramPurchased`; Notifications then queues the purchase confirmation email.

Do not use domain events for every trivial synchronous method call.

---

## 14. Identity

Core entities: `User`, `Role`, `Permission`, `RolePermission`, `UserRole`, `RefreshToken`, `EmailVerificationToken`, `PasswordResetToken`, `UserConsent`, `UserPreference`.

Initial roles: `Client`, `Expert`, `Administrator`. Roles are convenience groups. Authorization must use explicit permissions, e.g.:

```text
content.view / content.create / content.edit / content.publish
questionnaire.submit / questionnaire.review / questionnaire.answer
events.view / events.manage
chat.use / chat.moderate
billing.view / billing.manage
users.manage
audit.view
```

Avoid authorization logic such as `if (user.Role == "Expert")` scattered throughout controllers.

---

## 15. Program purchase and access architecture

Content owns programs and their educational structure. Billing owns commercial offers, immutable purchase records, payment state and program entitlement state explicitly.

```text
Billing
  ProgramOffer, ProgramPrice, Purchase,
  PaymentCustomer, Payment, Invoice, WebhookEvent, ProgramEntitlement
```

`ProgramOffer.ProgramId` is an opaque reference to a Content-owned program. There is no cross-module database foreign key and Billing never edits Content entities. The Content module does NOT create or own entitlement records. Billing is the single source of truth for program access rights. Other modules consume an abstraction such as `IProgramAccessContext` with `HasProgramAccessAsync(userId, programId)` / `RequireProgramAccessAsync(userId, programId)`.

For V1, keep entitlement logic specific: one `ProgramEntitlement` row represents one client's access to one program. Use a uniqueness constraint on `(UserId, ProgramId)`. Store `GrantedAtUtc`, nullable `RevokedAtUtc`, `Status` and `SourcePurchaseId`; do not add a generic feature-licensing engine. Permanent access normally has no end timestamp.

---

## 16. Purchase and entitlement states

`PurchaseStatus`: `Pending`, `Succeeded`, `Failed`, `Refunded`, `Chargeback`.

* **Pending / Failed** — no access is granted.
* **Succeeded** — an active permanent `ProgramEntitlement` is granted idempotently for the purchased program.
* **Refunded / Chargeback** — access may be revoked according to the explicit business operation; historical records remain intact.

V1 has no trials, recurring billing periods, grace periods, cancellation-at-period-end or automatic entitlement expiration.

---

## 17. Payment lifecycle

Stripe is the initial provider. Never trust payment status reported by the browser.

```text
Client selects ProgramOffer → Checkout Session → Stripe → Webhook → Billing module → Purchase → ProgramEntitlement
```

The checkout-success redirect is informational only. Only validated, idempotently processed provider webhooks may mark a purchase successful and grant program access. The amount, currency, offer and program are resolved from server-owned records; browser-reported commercial data is never trusted.

Webhook requirements: signature validation; unique provider event ID; idempotent processing; retry safety; raw event storage where appropriate; handling out-of-order events; structured audit trail.

---

## 18–22. Content model

```text
Domain
  Program
    Section
      ContentItem
```

Initial domains: Psychology, Sport, Nutrition, Business, FinancialEducation. Do not create separate engines per domain.

**Program**: `Id, DomainId, Slug, Status, DefaultLanguage, CoverAssetId, SortOrder, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, ConcurrencyToken`. Status: `Draft, Published, Archived`. Translated fields: `Title, ShortDescription, Description`.

**Section**: ordered within a Program (seeded default of five, not a schema constraint — expert may add/remove/reorder). `Id, ProgramId, SortOrder, Status, CreatedAt, UpdatedAt`. Translated fields: `Title, Description`.

**ContentItem**: V1 supports only `Video` and `RichText`. `Id, SectionId, Type, SortOrder, IsRequired, MediaAssetId?, CreatedAt, UpdatedAt`. Translated fields may include `Title, Body` depending on type. Do not implement a fully dynamic content-type registry in V1; design cleanly enough that future types can be added through explicit handlers/components. Avoid speculative plugin architectures.

**MediaAsset**: `Id, Provider, ProviderAssetId, ProviderPlaybackId, DurationSeconds, ThumbnailUrl, ProcessingStatus, CreatedAt, UpdatedAt`. Processing status: `Uploading, Processing, Ready, Failed`. Signed playback authorization must validate an active `ProgramEntitlement` for the content item's owning program before generating the playback token or URL.

---

## 23–24. Progress

V1 progress entities: `ContentProgress`, `SectionProgress`.

**ContentProgress**: `Id, UserId, ContentItemId, Status, LastVideoPositionSeconds, WatchPercentage, StartedAt, CompletedAt, UpdatedAt`. Status: `NotStarted, InProgress, Completed`.

Completion rules:
* **Video** — automatically completed after ~90% watched. Progress persisted periodically (~every 15s) plus on pause, page navigation, player close, and video completion.
* **Rich text** — user explicitly selects "Mark as completed".

Do not persist a separate `ProgramProgress` table in V1 unless performance measurements prove it necessary — derive program progress from `SectionProgress` / `ContentProgress`. `SectionProgress` may remain denormalized for efficient dashboard rendering. Ensure recalculation is deterministic.

---

## 25–28. Questionnaire and guidance

Entities: `Questionnaire/QuestionnaireTranslation`, `Question/QuestionTranslation`, `QuestionOption/QuestionOptionTranslation`, `QuestionnaireSubmission`, `QuestionnaireAnswer`, `GuidanceResponse`, `GuidanceFollowUp`. Every published questionnaire belongs to a `ProgramId`; starting, resuming, submitting and reading guidance require access to that program.

Question types: `Text, LongText, SingleChoice, MultiChoice, Scale`.

Flow: Client starts questionnaire → Draft submission → save/resume → Submit → Expert queue → Expert reviews → Expert writes personalized guidance → Guidance published → Client notified → Client reads guidance. One bounded follow-up question is allowed after a guidance response — this is NOT direct messaging.

`QuestionnaireSubmission` operational timestamps: `CreatedAt, StartedAt, SubmittedAt, AssignedAt, ReviewedAt, AnsweredAt` (retained even with a single expert, to enable metrics: average response time, oldest unanswered submission, submissions waiting >24h/>48h, monthly expert workload).

Expert dashboard must include a visible submission queue: `Client, Program/Context, Submitted At, Waiting Time, Status, Last Activity, Action`.

**GuidanceResponse**: `Id, QuestionnaireSubmissionId, AuthorUserId, Version, Body, CreatedAt, PublishedAt, UpdatedAt`. Do not overwrite published guidance silently — if edited after publication, preserve history via a simple version number.

---

## 29–31. Events

Implement before Chat if scope/schedule is constrained.

Entities: `Event/EventTranslation`, `EventRegistration`, `EventReminder`.

**Event**: `Id, StartsAtUtc, EndsAtUtc, DisplayTimezone, LocationType, Location, MeetingUrl, Capacity, Status, CreatedAt, PublishedAt`. LocationType: `Online, Physical`. Status: `Draft, Published, Canceled, Completed`.

An event may be public to all authenticated clients or associated with one or more programs. Registration states: `Registered, Waitlisted, Canceled`. Rules: registration closes when the event begins; optional capacity; if full, new registration becomes Waitlisted; on cancellation, promote the oldest eligible waitlisted user; registration for a program-associated event requires access to at least one associated program.

Default reminders: 24h before, 1h before, via Hangfire. Jobs must be idempotent, retryable, locale-aware, timezone-aware, observable, and respect notification preferences.

---

## 32. Notifications

V1 implements email notifications only, behind a small channel abstraction `INotificationSender`. Do NOT implement SMS or push providers.

Types: `EmailVerification, PasswordReset, Welcome, ProgramPurchased, ProgramAccessRevoked, PaymentFailed, QuestionnaireSubmitted, GuidancePublished, EventRegistrationConfirmed, EventReminder, ChatPinnedMessage`.

Security and transactional notifications cannot be disabled. Marketing/community notifications may be disabled.

---

## 33–34. Chat scope

Admin-managed program rooms only in V1. A room must reference a program, and only clients entitled to that program may read or post in it. A program may have zero or more predefined rooms; clients cannot create rooms.

No: direct messages, attachments, voice messages, threads, reactions, group creation, private rooms.

Features: text messages, pagination, basic unread state, pinning, delete moderation, temporary mute, reporting, soft delete. Use SignalR if it stays straightforward; polling is acceptable if SignalR becomes a launch blocker — do not delay release for real-time perfection.

Every group-chat room must show a persistent localized notice that it is a shared area for clients who purchased that program, warning against posting sensitive health/financial/personal information.

---

## 35. Privacy and sensitive questionnaire data

Treat questionnaire data as high-sensitivity regardless of formal GDPR Article 9 classification (legal classification validated separately before launch).

Requirements: explicit questionnaire consent with versioning; restrictive authorization; no questionnaire data in ordinary analytics; no questionnaire content in logs or notifications; audit sensitive reads; encryption at rest for questionnaire responses and guidance where feasible; self-service export; deletion workflow; documented retention policy.

Questionnaire submissions and guidance are visible only to the submitting client and the authorized expert. Administrators should not automatically receive access unless explicitly required for support/security workflows.

---

## 36. Crisis-related behavior

Do NOT build automated clinical-risk classification, and do NOT automatically analyze questionnaire or chat messages for psychological crisis detection.

Instead: localized safety/disclaimer information on psychology-related pages; clearly visible emergency/help information where appropriate; allow users to report concerning community content; allow the expert to manually escalate per documented procedures. Never present the platform as diagnostic, treatment or emergency-response service.

---

## 37. Audit

Audit business-critical and security-relevant actions, e.g.: `user.login, user.failed_login, user.password_reset, user.role_changed, program_offer.created, program_offer.updated, purchase.created, purchase.succeeded, purchase.refunded, program_access.granted, program_access.revoked, payment.webhook_processed, questionnaire.submitted, questionnaire.read, guidance.published, content.published, event.published, event.canceled, chat.message_moderated, chat.user_muted`.

`AuditLog`: `Id, ActorUserId, Action, EntityType, EntityId, TimestampUtc, CorrelationId, IpAddress (where justified), Metadata`. Never record secret tokens or questionnaire text.

---

## 38. Admin and cross-module reporting

Administrative screens may use dedicated cross-module read models (e.g. `CustomerPurchaseAdminView` combining `Identity.User`, `Billing.Purchase`, `Billing.ProgramEntitlement`, the Content-owned program title, progress summary and last activity). Must be read-only, explicit, documented, optimized separately, and prohibited from changing module-owned state.

---

## 39–40. Frontend architecture

```text
src/
  app/{providers, router, query-client}/
  routes/
  layouts/
  modules/{auth, dashboard, content, player, questionnaire, billing, events, chat, admin}/
  shared/{api, auth, permissions, components, forms, hooks, i18n, formatting, validation, design-system}/
  locales/{ro, en}/
```

Avoid a giant shared `components` folder containing feature-specific UI — feature-specific components belong inside their module.

Client navigation: `Home, Programs, Events, Community, My Guidance, Billing, Profile`. Mobile prioritizes: `Home, Programs, Events, Community, Profile` with bottom/compact navigation.

---

## 41–44. Client UI (dashboard, programs, program detail, player)

**Dashboard** should feel premium, calm and goal-oriented (not enterprise admin-like). Hierarchy: Hero/Continue card (program, section, progress, remaining content, CTA); Personalized guidance (latest response preview, or "under review" state); Progress overview (overall completion, active programs, recently completed sections — avoid excessive charts); Upcoming event; optional lightweight Community activity.

**Programs screen**: title, intro, domain filter, program cards (cover, domain, title, short description, price, ownership/lock state, progress when owned, CTA: View/Buy/Start/Continue/Completed).

**Program detail**: public commercial header (cover, domain, title, description, active offer and price, Buy CTA when not owned); overview; curriculum preview. Full item bodies, playback data, questionnaires, progress and associated features remain protected until purchase. Owned programs show progress and Start/Continue actions instead of Buy.

**Program player** — desktop: header, left curriculum sidebar, right current-content pane (video/rich text), previous/next-or-complete footer. Mobile: header, progress, current content, previous/next, curriculum drawer (not a shrunk sidebar).

---

## 45–54. Expert/admin UI

Navigation: `Dashboard, Programs, Questionnaires, Events, Community, Subscribers, Billing, Notifications, Audit, Settings`.

**Expert dashboard** emphasizes actions requiring attention: pending questionnaires, oldest unanswered submission, upcoming events, recent purchases/refunds, reported chat messages, recent published content. KPI cards: customers with purchases, completed purchases, pending questionnaires, upcoming events, purchase revenue. Avoid vanity metrics.

**Program management**: `All / Drafts / Published / Archived`. Table columns: Title, Domain, Sections, Language coverage, Status, Updated, Actions (Edit, Preview, Publish/Unpublish, Duplicate, Archive).

**Program editor** — three-area layout: left Structure (sections/content items, add/duplicate/delete/reorder), center Editor (rich text editor, video configuration, section/program metadata), right Properties (language, status, required, ordering, translation status). Include a commercial area where authorized administrators create/edit a `ProgramOffer`, set its one-time `ProgramPrice` and currency, activate/deactivate sales and inspect validation preventing purchase without an active offer. Support drag-and-drop where appropriate.

**Translation management** is contextual within the editor (Romanian: Complete / English: Missing description, etc.) — no separate translation-management app.

**Questionnaire management**: builder (question list, add/reorder, question editor with type/label/help text/required/options/scale range/translation, preview, publish). Expert submission queue with strong visual aging warning (<24h normal, 24–48h attention, >48h overdue).

**Guidance editor**: client summary, questionnaire answers as question/answer cards, submission timeline, guidance editor, previous guidance versions, publish action.

**Events management**: list (Title, Date, Type, Registrations, Capacity, Status, Actions); editor (title, description, translations, date/time, display timezone, online/physical, meeting link/location, capacity, publication status, reminder settings); details (registered users, waitlist, attendance, reminders).

**Chat moderation**: Reported Messages, Muted Users, Recent Moderator Actions. Per report: message context, author, reporter, reason, timestamp. Actions: Dismiss, Delete Message, Mute User. Keep tooling simple in V1.

**Billing administration**: offers by program; customer purchase table (Customer, Email, Program, Purchase Status, Amount, Currency, Purchased At, Access Status); purchase detail (offer and price snapshot, provider payment id, payment, invoice, program entitlement, refund/chargeback state, webhook timeline). Offer creation and editing require `billing.manage`. Do not expose raw webhook payloads in ordinary support UI unless restricted to technical administrators.

---

## 55–59. Design system and accessibility

Communicate trust, calm, progress, expertise, structure, personal growth. Avoid overly playful gamification, excessive gradients/card nesting, dashboard overload, childish illustrations, generic Bootstrap-like appearance.

Tokens: `Background, Surface, SurfaceRaised, BorderDefault, BorderStrong, TextPrimary, TextSecondary, TextMuted, Primary, PrimaryHover, Success, Warning, Danger, Info`, plus spacing/typography/radius/shadow scales, breakpoints, focus ring, motion timing. All components use tokens, not arbitrary styling.

Core reusable components (minimum): Button, IconButton, Input, Textarea, Select, Checkbox, Radio, Switch, Slider, DatePicker, Card, Badge, Avatar, Tabs, Breadcrumbs, Modal, Drawer, Dropdown, Tooltip, Table, Pagination, EmptyState, Skeleton, Alert, Toast, ProgressBar, StatusBadge, RichTextEditor, LanguageSwitcher, TranslationStatus, VideoPlayer, ContentNavigation, ConfirmationDialog, ErrorBoundary.

Responsive targets: Mobile, Tablet, Laptop/Desktop. Convert management tables to stacked cards/condensed rows/drawers/context menus on mobile rather than shrinking them. Touch targets ≥44px.

Accessibility target: WCAG 2.2 AA — keyboard navigation, visible focus states, semantic HTML, labels, ARIA only where necessary, sufficient contrast, accessible dialogs/tables, captions/subtitles for video, reduced-motion preference.

---

## 60–61. API conventions and error contract

Routes: `/api/v1/{auth, users, programs, progress, questionnaires, events, chat, billing, admin}`.

Rules: plural resources; predictable routes; DTOs only; FluentValidation; consistent pagination; sorting/filtering; cancellation tokens; resource-level authorization; standardized errors.

Error contract example:

```json
{ "code": "SUBSCRIPTION_INACTIVE", "messageKey": "errors.subscriptionInactive", "correlationId": "..." }
```

Validation error example:

```json
{
  "code": "VALIDATION_FAILED",
  "messageKey": "errors.validationFailed",
  "errors": [{ "field": "email", "code": "INVALID_EMAIL", "messageKey": "validation.invalidEmail" }]
}
```

Backend does not return localized prose as the primary contract.

---

## 62–64. Database, money, dates

For every important entity: primary key, foreign keys, indexes, unique constraints, nullable rules, deletion behavior, audit timestamps, creator/updater where relevant, concurrency token where useful. Always index foreign keys; add indexes based on actual common queries, not mechanically on every column.

Money: always `decimal`, never `float`/`double`, for prices/totals/billing values. Store currency explicitly.

Dates: store timestamps in UTC. User timezone defaults to `Europe/Bucharest`, configurable per profile. Events keep `StartsAtUtc, EndsAtUtc, DisplayTimezone`. Formatting follows current UI locale and timezone.

---

## 65. Security

Strong password hashing; email verification; reset tokens; refresh-token rotation and hashing; token revocation; rate limiting; secure CORS; resource ownership validation; permission checks; file validation; secure provider secrets; webhook signature verification; account lockout/abuse protection where appropriate; audit logging.

Never log: access tokens, refresh tokens, passwords, questionnaire content, payment card data, sensitive guidance text.

---

## 66. Data deletion and export

Self-service data export as a JSON archive plus user-owned attachments where applicable. Deletion distinguishes hard delete, anonymization, and retained legally-required billing records. For chat, do not destroy conversation continuity — replace a deleted user's identity with an anonymized representation where appropriate. Document retention decisions.

---

## 67. Performance target

Design for ~2,000 customers with purchases / ~200 concurrent users. Do not optimize for millions. Targets: low-hundreds-of-ms typical API requests; efficient dashboard queries; paginated chat; CDN video delivery; usage-based indexes; no premature distributed caching.

---

## 68. Testing priorities

Highest-risk coverage: **Billing** (server-owned price selection, webhook signature validation, idempotency, concurrent duplicate delivery, out-of-order events, refund and chargeback handling); **Program entitlements** (cross-program denial, permanent access, duplicate purchase protection, revocation without data deletion); **Security** (endpoint permissions, cross-user and cross-program access attempts, questionnaire privacy); **Questionnaires** (draft, submission, guidance, versioning, bounded follow-up); **Localization** (translation lookup, fallback); **Events** (program association, capacity, waitlist, promotion, reminder scheduling, timezone handling); **Content progress** (video resume, completion threshold, rich-text manual completion).

---

## 69. Delivery order

* **Phase 1 — Foundation**: solution structure, PostgreSQL, authentication, users, roles, permissions, localization infrastructure, design system, logging, audit, Docker, CI foundation. *Deliverable: production-shaped skeleton where users can register, verify email, log in and navigate localized UI.*
* **Phase 2 — Content**: domains, programs, sections, Video/RichText content, translations, admin content authoring, video-provider integration, client program UI, progress tracking. *Deliverable: expert can publish programs, clients can consume them.*
* **Phase 3 — Program commerce and access**: admin-managed program offers and one-time prices, Stripe Checkout, webhook handling, purchases, invoices, permanent ProgramEntitlement records and per-program access gating. *Deliverable: clients can buy programs separately and access only what they purchased.*
* **Phase 4 — Questionnaire and guidance**: questionnaire builder, localized questions, drafts, submissions, expert review queue, guidance, versioning, one follow-up, sensitive-data protection. *Deliverable: expert-led personalization works end-to-end.*
* **Phase 5 — Events**: event authoring, translations, optional program association, registration, capacity, waitlist, email reminders. *Deliverable: eligible clients discover and register for live activities.*
* **Phase 6 — Community**: program-associated rooms, SignalR or polling, reports, moderation, mute, pin, privacy notice. *May move after launch under delivery pressure.*
* **Phase 7 — Launch readiness**: expert dashboard, admin views, GDPR export/delete, accessibility, performance, error monitoring, production configuration, backup strategy, deployment.

---

## 70. Out of scope

Organizations; multi-tenancy; marketplace; multiple independent experts; expert payouts; native apps; SMS; push notifications; direct messaging; video calling; calendar booking; adaptive learning; AI advice/therapy/financial advice; program branching; drip schedules; certificates; badges; gamification; habits engine; goals engine; journal module; advanced assessment scoring; social feed; followers; custom workflows; enterprise SSO; report builder; generic plugin system; microservices. Do not create unused entities for them.

---

## 71. Future extension philosophy

Future-readiness means clear module boundaries, sensible contracts, stable domain ownership, normal relational modeling, versionable APIs, localization, permission-based access, provider abstractions where external vendors are involved — not implementing future features. Prefer refactoring later over speculative abstraction now.

---

## 72. Implementation discipline

No placeholder implementations; no unfinished TODO-driven architecture; complete files when implementation is requested; domain/business logic outside controllers; data access outside React components; no EF entities exposed directly; no business logic embedded in UI components; async database/external calls; cancellation tokens; input validation; authorization; structured logs; transactions around critical state changes; concurrency where needed; migration-based schema changes; no arbitrary magic strings; no duplicated permission names; no duplicated localization keys.

---

## 73. Architecture documentation

Short ADRs for important choices, at minimum:

```text
ADR-001 Modular Monolith
ADR-002 PostgreSQL
ADR-003 Subscription Entitlement Ownership
ADR-004 UI vs Content Localization
ADR-005 Video Hosting Provider Abstraction
ADR-006 Questionnaire Sensitive Data Handling
ADR-007 Controlled Cross-Module Read Models
ADR-008 Transactional Outbox Usage
```

Keep ADRs concise; document reasoning and trade-offs.

---

## 74. Expected architecture output before coding

Executive architecture overview; module map; module ownership table; allowed dependency map; domain event map; database schema; ER relationship overview; key indexes; subscription state machine; entitlement flow; payment webhook sequence; authentication/token lifecycle; permission matrix; questionnaire lifecycle; progress calculation rules; event registration/waitlist state machine; localization architecture; sensitive-data strategy; audit strategy; frontend route map; client UI information architecture; expert/admin UI information architecture; design-system tokens; responsive rules; API contract overview; background-job catalogue; testing strategy; deployment architecture; phase-by-phase backlog; architectural risks and trade-offs.

---

## 75. Architecture review requirement

Before implementation, actively challenge the proposed architecture. Identify anything unnecessary, overengineered, underspecified, too tightly coupled, too generic, likely to cause implementation delays, or likely to create maintenance problems. For every issue: Issue / Why it matters / Recommended change / Trade-off. Do not approve architectural choices merely because they were included in this prompt — prefer the simplest architecture that satisfies the real V1 business requirements.

---

## 76. Implementation execution

Implement incrementally after architecture approval — do NOT generate the full application in one giant response. Per phase: state the objective; list affected modules; show schema changes; show required permissions; define API contracts; define frontend routes; implement backend; implement frontend; add localization resources; add automated tests; add migrations; provide local verification commands; provide a manual acceptance checklist; stop after the phase is complete. The application must remain buildable and runnable after every phase.

---

## 77. Final product objective

The resulting V1 must feel like a complete commercial product, not an architectural prototype.

The **expert/administrator** must be able to: manage programs; publish multilingual educational content; create and manage one-time program offers and prices; review client questionnaires; provide personalized written guidance; inspect customers, purchases, payments and program entitlements; publish events; moderate program communities.

The **client** must be able to: register; browse programs in the five domains; purchase programs separately; permanently access only purchased programs; resume video content; track progress; submit program questionnaires; receive expert guidance; register for eligible events; participate in purchased-program communities; inspect purchases and invoices; manage profile and language.

The system must be secure, maintainable, responsive and multilingual.

**Most importantly: keep V1 simple enough to actually finish.**
