# B-United — Personal Development Platform (V1)

Single-organization, subscription-based personal development platform.
One expert, five content domains (Psychology, Sport, Nutrition, Business,
Financial Education), structured programs, questionnaire-driven expert
guidance, progress tracking, community chat and events.

Full product/architecture specification: [`docs/PROMPT.md`](docs/PROMPT.md).
Architecture decisions: [`docs/adr/`](docs/adr/).

> Status: repository scaffolding only. No implementation yet — see
> `docs/PROMPT.md` sections 74–76 for the required architecture review and
> phased delivery plan before writing production code.

## Repository structure

```text
src/                    ASP.NET Core modular monolith (backend)
  BuildingBlocks/        Shared cross-cutting code (no business logic)
  Modules/                One folder per bounded module (Domain/Application/Infrastructure/Api/Contracts/Tests)
  Api/                     Web API host process
  Jobs/                    Hangfire background jobs
  Migrations/              EF Core migration history

frontend/               React + TypeScript + Vite SPA
  src/app/                 Providers, router, query client bootstrapping
  src/routes/              Route definitions
  src/layouts/             Page layouts (client / expert-admin)
  src/modules/             Feature modules (auth, dashboard, content, player, questionnaire, billing, events, chat, admin)
  src/shared/              Cross-module frontend building blocks (api, auth, permissions, components, forms, hooks, i18n, formatting, validation, design-system)
  src/locales/             UI translation resources (ro, en)

docs/
  PROMPT.md               Full product/architecture specification
  adr/                     Architecture Decision Records
```

## Modules (backend)

| Module         | Owns                                                                 |
|----------------|-----------------------------------------------------------------------|
| Identity       | Users, roles, permissions, tokens, consent, preferences               |
| Content        | Domains, Programs, Sections, ContentItems, MediaAssets + translations |
| Progress       | ContentProgress, SectionProgress, derived program progress            |
| Questionnaires | Questionnaires, submissions, answers, expert guidance                 |
| Billing        | Plans, subscriptions, payments, invoices, PlatformAccess entitlement  |
| Notifications  | Email notification sending abstraction                                |
| Events         | Events, registrations, waitlist, reminders                            |
| Chat           | Fixed community rooms, messages, moderation                           |
| Files          | Object storage abstraction for non-video assets                       |
| Audit          | Business-critical / security-relevant action log                      |
| Admin          | Read-only cross-module admin/reporting projections                    |

See each module's `README.md` under `src/Modules/<Module>/` for layering rules.

## Technology stack

- **Backend:** ASP.NET Core, C#, EF Core, FluentValidation, PostgreSQL, Hangfire, Serilog, JWT + rotating refresh tokens, permission-based authorization.
- **Frontend:** React, TypeScript, Vite, React Router, TanStack Query, React Hook Form, Zod, Zustand (minimal), Tailwind CSS, i18next/react-i18next.
- **Infrastructure:** Docker / Docker Compose, PostgreSQL, video-provider abstraction (Mux/Cloudflare Stream/Vimeo), Stripe (payments), transactional email abstraction.

## Next steps

1. Review `docs/PROMPT.md` section 74/75 and produce the architecture review deliverables before writing implementation code.
2. Phase 1 (Foundation): initialize the .NET solution and module projects, the Vite/React app, Docker Compose (PostgreSQL), authentication, localization infrastructure and CI.
