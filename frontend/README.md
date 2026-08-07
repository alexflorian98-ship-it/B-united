# Frontend

React + TypeScript + Vite single-page application.

- `src/app/` — providers, router and TanStack Query client bootstrapping.
- `src/routes/` — route definitions (client area + expert/admin area).
- `src/layouts/` — page shells (client layout, expert/admin layout).
- `src/modules/` — one folder per feature (auth, dashboard, content, player,
  questionnaire, billing, events, chat, admin). Feature-specific components,
  hooks and API calls live inside their module, not in `shared/`.
- `src/shared/` — cross-module building blocks only: api client, auth
  context, permission helpers, generic components, form helpers, hooks,
  i18n setup, formatting (dates/money), validation schemas, design-system
  tokens/primitives.
- `src/locales/` — UI translation resources (`ro` default, `en`). Business
  content translations (programs, events, questionnaires) are stored in the
  database, not here — see `docs/PROMPT.md` section 6.
