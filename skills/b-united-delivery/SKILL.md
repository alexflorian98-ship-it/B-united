---
name: b-united-delivery
description: Plan, implement, review, secure, test, and release B-United V1 vertical slices. Use for architecture decisions, ASP.NET Core backend work, React frontend work, PostgreSQL schema changes, security/privacy reviews, quality audits, CI/deployment work, or coordinated delivery across these areas in the B-United repository.
---

# B-United Delivery

Deliver one production-ready vertical slice at a time. Preserve the V1 scope, modular-monolith boundaries, and English-only technical implementation.

## Start every task

1. Read `README.md`, the relevant parts of `docs/PROMPT.md`, and applicable ADRs.
2. Inspect the current implementation and working-tree state. Do not infer implementation from scaffolding README files.
3. State the concrete objective, affected modules, and acceptance criteria.
4. Select the smallest required roles from [roles.md](references/roles.md).
5. Read [quality-gates.md](references/quality-gates.md) before changing production code.

Do not spawn subagents unless the user explicitly requests delegation, agents, or parallel work. When delegation is authorized, assign non-overlapping ownership and retain final integration responsibility.

## Scope guardrails

- Build a single-organization modular monolith and one React SPA.
- Do not add multi-tenancy, microservices, marketplaces, generic plugin engines, distributed infrastructure, or other out-of-scope systems.
- Prefer a direct implementation over an abstraction without a current V1 consumer.
- Keep business rules outside controllers and React presentation components.
- Allow cross-module dependencies only through Contracts. Never reference another module's Domain or Infrastructure layer.
- Allow cross-module database joins only for explicit read-only admin/dashboard projections.
- Use the transactional outbox only where loss or retry of an important cross-module event matters.
- Keep UI localization in source-controlled i18next files and business-content localization in dedicated translation tables.
- Enforce access, permissions, ownership, and entitlement server-side.

## Delivery workflow

### 1. Design

- Challenge ambiguity, coupling, and needless ceremony.
- Define schema changes, permissions, API contracts, frontend routes, events, and sensitive-data handling.
- Record or update an ADR only for a durable architectural decision.

### 2. Implement

- Implement a thin end-to-end slice that remains buildable.
- Add validation, authorization, structured errors, cancellation, logging, localization resources, and migrations where applicable.
- Never add placeholder production implementations or unresolved TODO architecture.

### 3. Review

- Perform an independent security/privacy pass for authentication, authorization, billing, questionnaires, files, chat moderation, and administrative operations.
- Review schema integrity and migration safety for every database change.
- Check module dependency direction and API/DTO boundaries.

### 4. Verify

- Run focused tests first, then the broadest practical build/test suite.
- Include negative authorization, cross-user access, boundary, retry, idempotency, and concurrency cases where relevant.
- Report exact commands, results, remaining risks, and unverified items.

### 5. Hand off

Lead with the delivered outcome. List material files changed, verification performed, and any genuine blocker or follow-up. Do not call incomplete work complete.

## Security invariants

- Never trust browser-reported payment or access state.
- Only validated, idempotently processed provider webhooks may activate subscription access.
- Billing exclusively owns `PlatformAccess`; other modules consume `IAccessContext`.
- Never log secrets, tokens, passwords, questionnaire answers, guidance text, card data, or raw sensitive payloads.
- Treat questionnaire submissions and guidance as high-sensitivity data; administrators receive no implicit access.
- Validate resource ownership on every user-scoped read and mutation.
- Preserve account and historical data when subscription access expires.

## Technical conventions

- Write code, identifiers, migrations, comments, logs, tests, technical documentation, branches, and commit suggestions exclusively in English.
- Store timestamps in UTC and format them using the user's locale and timezone.
- Use `decimal` plus an explicit currency for money.
- Return DTOs and stable error codes/message keys, never EF entities or localized backend prose.
- Keep Romanian as the default UI locale and maintain Romanian/English translation-key parity.

