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

## Local development setup

### Database

The Api host needs a `ConnectionStrings__Default` PostgreSQL connection string,
supplied via a repo-root `.env` file (copy `.env.example` to `.env` and fill
in real values — `.env` is git-ignored and must never be committed).

Two ways to provide PostgreSQL locally:

- **Docker Compose (recommended for a clean, disposable instance):**
  ```
  docker compose up -d postgres
  ```
  This starts `postgres:16-alpine` on `POSTGRES_PORT` (default `5432`) using
  the `POSTGRES_DB`/`POSTGRES_USER`/`POSTGRES_PASSWORD` values from `.env`.

- **An existing local PostgreSQL install:** create a dedicated role and
  database instead of reusing another project's:
  ```sql
  CREATE USER bunited WITH PASSWORD '<choose-a-password>';
  CREATE DATABASE bunited OWNER bunited;
  ```
  Then set `ConnectionStrings__Default` in `.env` to match, e.g.:
  ```
  ConnectionStrings__Default=Host=localhost;Port=5432;Database=bunited;Username=bunited;Password=<choose-a-password>
  ```

### Running the Api host

The Api host loads `.env` automatically at startup (via `DotNetEnv`, searching
upward from the working directory) and fails fast if
`ConnectionStrings__Default` is missing:

```
dotnet run --project src/Api/BUnited.Api.csproj
```

By default this listens on `http://localhost:5000` (`launchSettings.json`) and
only accepts cross-origin requests from `http://localhost:5173` in the
`Development` environment (`appsettings.Development.json`'s
`Cors:AllowedOrigins`) — see `src/BuildingBlocks/Security/Cors/CorsExtensions.cs`.
Running via `dotnet run --no-launch-profile` skips `launchSettings.json`
entirely, which also skips the `ASPNETCORE_ENVIRONMENT=Development` it sets —
pass `ASPNETCORE_ENVIRONMENT=Development` explicitly in that case, or the SPA
below won't be able to reach the Api (its requests will be rejected by CORS).

### Running the frontend

```
cd frontend
cp .env.example .env   # defaults to http://localhost:5000/api/v1, matching the Api host above
npm install
npm run dev
```

This starts the Vite dev server on `http://localhost:5173`. The Api host must
already be running (see above) for registration/login/etc. to work — the SPA
makes real HTTP calls, it has no mock backend mode.

## Next steps

1. Review `docs/PROMPT.md` section 74/75 and produce the architecture review deliverables before writing implementation code.
2. Phase 1 (Foundation): initialize the .NET solution and module projects, the Vite/React app, Docker Compose (PostgreSQL), authentication, localization infrastructure and CI.
