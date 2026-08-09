# ADR-007: Controlled Cross-Module Read Models

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

Read-only administrative/dashboard projections (e.g. SubscriberAdminView) may join data owned by multiple modules directly, for simplicity. These read models must remain read-only, live in dedicated query code, and must never become a hidden business dependency or mutate another module's data.

## Consequences

Phase 7.A (docs/TASKS.md P7.01–P7.03, docs/PROMPT.md §442) is the first implementation of this
decision: `BUnited.Modules.Admin.Application.UseCases.GetDashboardHandler` reads directly from
`Purchase`/`ProgramEntitlement` (Billing), `QuestionnaireSubmission` (Questionnaires), `Event`
(Events), `Report` (Chat), and `Program` (Content) through the shared `DbContext`, resolving
client identities via `IUserLookup` rather than Identity's Domain layer. This required
`Admin.Application` to take `ProjectReference`s to those five modules' Domain projects — an
explicit, documented exception to "never reference another module's Domain layer" (CLAUDE.md),
scoped to this one module. Every query is `AsNoTracking`; `GetDashboardHandlerTests` includes a
regression test asserting the handler leaves the `ChangeTracker` clean and every row count
unchanged. The Admin module (previously an empty scaffold, see its `README.md`) is now the
designated home for this class of projection — future admin/dashboard read models should extend
`GetDashboardHandler`'s pattern rather than duplicating cross-module query logic inside another
module.
