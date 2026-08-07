# Quality gates

A slice is complete only when every applicable gate passes or the exception is explicitly documented.

## Architecture

- Module ownership is explicit.
- No forbidden Domain/Infrastructure cross-reference or circular dependency exists.
- Added abstractions have a current V1 consumer.
- ADRs and contracts match the implementation.

## Backend and data

- Build succeeds and migrations apply to a clean PostgreSQL database.
- DTOs, FluentValidation, cancellation tokens, stable errors, and structured logs are present.
- Foreign keys, indexes, uniqueness, nullability, deletion behavior, UTC, currency, and concurrency are intentional.
- Mutations use transactions where partial completion would violate an invariant.

## Security and privacy

- Authentication, permission, ownership, and entitlement checks execute server-side.
- Anonymous, wrong-permission, wrong-owner, expired-access, and tampered-input cases are tested.
- Webhooks verify signatures and handle duplicates, retries, and out-of-order events where applicable.
- Logs, errors, analytics, notifications, and audit metadata contain no prohibited sensitive content.
- Uploads validate type, size, storage key, authorization, and download/playback access.

## Frontend

- Loading, empty, error, success, and unauthorized states are deliberate.
- UI strings use i18next and `ro`/`en` keys remain in parity.
- Layout works on mobile, tablet, and desktop without blindly shrinking management tables.
- Keyboard, focus, semantics, labels, dialog behavior, contrast, and reduced motion are verified.

## Testing and operations

- Focused automated tests pass; broader build/test commands run when practical.
- High-risk rules have negative and boundary tests, not only happy paths.
- Configuration fails safely in production when required secrets are absent or use known placeholder values.
- Health checks, observability, deployment steps, migration strategy, and rollback implications are updated when affected.

## Handoff evidence

Report delivered behavior, material files changed, commands and outcomes, the security/privacy review result, and remaining risks or unverified behavior.
