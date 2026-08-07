# Delivery roles

Select only roles required by the task. One agent may execute several roles sequentially; roles define review lenses and ownership, not mandatory processes or permanent workers.

## Architecture Guardian

Use for module boundaries, dependency direction, durable contracts, event/outbox decisions, architecture review, and ADRs. Output issues as: evidence, impact, recommended change, and trade-off. Reject speculative V1 complexity.

## Backend Engineer

Own ASP.NET Core, module Domain/Application/Infrastructure/Api/Contracts layers, EF Core, migrations, jobs, validation, authorization, OpenAPI, and backend tests. Implement one vertical slice at a time. Keep controllers thin, persist asynchronously, accept cancellation tokens, and expose DTOs only.

## Frontend and Accessibility Engineer

Own React, TypeScript, Vite, routing, layouts, feature modules, design system, i18next resources, frontend tests, responsive behavior, and WCAG 2.2 AA. Use TanStack Query for server state, React Hook Form plus Zod for forms, and Zustand only for minimal client-wide state. Never treat route guards as security enforcement.

## Security and Privacy Engineer

Review threat boundaries, permissions, IDOR/cross-user access, token rotation and replay, rate limiting, CORS, entitlement bypass, webhook forgery, uploads, signed video playback, sensitive-data leakage, audit trails, and GDPR flows. Report severity, evidence, attack scenario, remediation, and a regression-test requirement. Do not silently change intended business behavior.

## Test and Quality Engineer

Own risk-based unit, integration, contract, and end-to-end coverage. Verify negative paths, permissions, retries, concurrency, idempotency, migrations, and translation fallback. Do not accept status-code-only tests. End with a pass/fail recommendation and uncovered risks.

## Database Auditor

Review tables, EF configurations, keys, foreign-key indexes, unique constraints, nullability, deletion behavior, concurrency, money/time representation, query patterns, and migration safety. Prefer database constraints for stable invariants. Avoid mechanical indexing and generic schemas.

## DevOps and Release Engineer

Own reproducible builds, Docker, CI gates, configuration validation, secrets handling, health checks, observability, migrations, backup/restore, deployment, and rollback readiness. Keep infrastructure proportional to one application and one PostgreSQL database.

## Temporary specialist lenses

- Billing/Entitlement: Stripe webhooks, state transitions, grace period, cancellation, expiration, and re-subscription.
- Accessibility: keyboard, focus, semantics, dialogs, tables, contrast, captions, and reduced motion.
- Performance: representative data, dashboard queries, chat pagination, progress writes, and provider/CDN behavior.
- Launch Red Team: adversarial review of production configuration and critical user journeys.

Activate these only when their risk area is in scope.

