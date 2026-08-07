# B-United

## Purpose

B-United is a commercially viable V1 personal-development platform for one organization and one primary expert. Subscribers receive access to multilingual programs, questionnaire-based written guidance, progress tracking, predefined community rooms, and events through a recurring subscription.

The full product specification is in `docs/PROMPT.md`. The delivery backlog is in `docs/TASKS.md`.

## Mandatory development instructions

@docs/DEVELOPMENT_INSTRUCTIONS.md

The imported instructions are mandatory for every task. If a request conflicts with them, stop before making changes, identify the conflict, and ask for an explicit decision. Never bypass a rule silently.

## Current status

The repository is an architecture scaffold. It contains the planned module structure, initial ADRs, locale namespaces, PostgreSQL Docker Compose configuration, and the B-United delivery skill. `docs/ARCHITECTURE.md` now contains the full Phase 0 architecture deliverable and the mandatory architecture review (§75), with four accepted findings (R1–R4). It is **pending explicit human approval (task P0.32)**. The ASP.NET Core solution and React application have not been initialized yet — do not begin Phase 1 implementation until that approval is recorded.

## Architecture

- One ASP.NET Core modular monolith and one PostgreSQL database.
- One React, TypeScript, and Vite SPA.
- Backend modules: Identity, Content, Progress, Questionnaires, Billing, Notifications, Events, Chat, Files, Audit, and Admin.
- Shared backend concerns live in `src/BuildingBlocks`; business logic must not.
- Each module owns its Domain, Application, Infrastructure, Api, Contracts, and Tests layers.
- Cross-module dependencies go through Contracts. Never reference another module's Domain or Infrastructure layer.
- Read-only cross-module queries are allowed only in explicit admin/dashboard read models.
- Billing exclusively owns subscription state and the `PlatformAccess` entitlement. Other modules consume `IAccessContext`.
- Use the transactional outbox only for important cross-module events that require retry or delivery guarantees.
- UI localization uses i18next locale files. Business-content localization uses dedicated database translation tables.
- Store video with a dedicated provider; issue short-lived playback access only after server-side authorization.

## Repository map

```text
src/BuildingBlocks/       Shared technical primitives
src/Modules/              Backend business modules
src/Api/                  Single ASP.NET Core host
src/Jobs/                 Hangfire jobs
src/Migrations/           EF Core migration history
frontend/src/             React SPA
frontend/src/locales/     Romanian and English UI resources
docs/adr/                 Architecture Decision Records
docs/TASKS.md             Phased implementation backlog
skills/b-united-delivery/ Project delivery skill
```

## Non-negotiable rules

- Keep V1 single-organization. Do not add multi-tenancy, microservices, marketplaces, generic plugin systems, or other speculative features.
- Write all code, identifiers, logs, tests, migrations, comments, and technical documentation in English.
- Romanian is the default UI language; maintain Romanian/English translation-key parity and never hardcode visible UI text.
- Enforce authentication, permissions, ownership, and subscription access server-side.
- Never expose EF entities through the API; use DTOs and stable error codes/message keys.
- Keep business logic outside controllers and React components.
- Use `decimal` with explicit currency for money and UTC for persisted timestamps.
- Never log passwords, tokens, questionnaire answers, guidance text, card data, secrets, or raw sensitive payloads.
- Treat questionnaire submissions and guidance as high-sensitivity data. Administrators have no implicit access.
- Add validation, cancellation, structured logging, authorization, migrations, and risk-based tests with every applicable slice.
- Preserve existing user work and unrelated changes. Do not use destructive Git commands without explicit authorization.

## Working method

1. Read the relevant prompt sections, ADRs, and backlog items before implementation.
2. Deliver one buildable vertical slice at a time.
3. State affected modules, schema, permissions, API contracts, frontend routes, and acceptance criteria.
4. Run focused tests, then the broadest practical build and test suite.
5. Include negative authorization and cross-user tests for protected resources.
6. Update `docs/TASKS.md`, relevant ADRs, and this file when architecture or conventions change.
7. Use `$b-united-delivery` in Codex for the complete delivery and quality-gate workflow.

## Session changes

This section is maintained automatically by `.claude/hooks/update-claude-md.ps1` when a Claude Code session stops. Entries describe workspace snapshots changed during that session, not committed release notes.

<!-- SESSION-CHANGES:START -->
No Claude Code session changes recorded yet.
<!-- SESSION-CHANGES:END -->
