# Handover: Global-Subscription → Per-Program Purchase Migration

Written for a fresh agent (ChatGPT or otherwise) with zero memory of the Claude Code session that did this work. Read this fully before touching anything. Also read `CLAUDE.md` and `docs/DEVELOPMENT_INSTRUCTIONS.md` — every rule in those two files is a release gate for this repo, not a suggestion.

## What this migration is

B-United's product architecture changed from **one global recurring subscription** unlocking the whole platform to **per-program one-time purchases** — a client buys each program separately and gets **permanent** access only to that program. This is documented in:

- `docs/adr/ADR-003-Subscription-Entitlement-Ownership.md` (revised 2026-08-09, accepted)
- `docs/PROMPT.md` §1-3, §15-17, §22, §25-34 (already updated to describe the new model)
- `docs/TASKS.md` Phase 3, section **3.G "Architecture correction: one-time program commerce"**, tasks **P3.33–P3.45** — this is the authoritative task breakdown. Check its checkboxes for the real per-subtask status; some are already ticked with evidence notes from this migration.

The full approved implementation plan is at `C:\Users\alexf\.claude\plans\twinkling-hugging-clarke.md` on the machine this session ran on — if you don't have filesystem access to that path, the plan's content is summarized accurately below; you don't strictly need the original file.

## Status as of this handover (updated 2026-08-09, all slices verified — see "First thing to do" to confirm nothing drifted after this was written)

Implemented as a sequence of verified, buildable vertical slices, each independently confirmed (build + full test suite + live curl verification against a real local Postgres instance) before moving to the next. **All 7 slices are now done.**

| Slice | Scope | Status |
|---|---|---|
| 1 | Billing domain/schema rewrite (`Plan`/`Subscription`/`Entitlement` → `ProgramOffer`/`ProgramPrice`/`Purchase`/`ProgramEntitlement`), EF migration, new `IProgramAccessContext` cross-module contract, checkout/webhook rewrite, Content video-playback + Events registration cut over to the new contract | **Done, verified** |
| 2 | Admin program-offer management backend (`AdminBillingController` offer routes), Content catalogue commercial DTOs + paywall stripping of protected content for non-owners | **Done, verified** |
| 3 | Progress module: closed a real, previously-exploitable gap — all 4 progress handlers now resolve the owning `ProgramId` and require entitlement before reading/writing progress | **Done, verified** |
| 4 | Questionnaires: `Questionnaire` gained a required `ProgramId`; all client-facing routes now require program access in addition to ownership checks; Expert review path deliberately left untouched | **Done, verified** |
| 5 | Events: new `EventProgram` many-to-many join (zero associations = public event, unchanged); Chat: `ChatRoom` converted from a fixed enum to a program-owned DB entity, legacy 6 rooms deactivated | **Done, verified** — its background agent hit a session usage limit mid-run before finishing its own live-verification pass, but its code changes had already landed cleanly (build+tests green). Independently re-verified afterward: created a real program-scoped chat room via curl, confirmed a fresh unentitled user is denied read/post with `PROGRAM_ACCESS_REQUIRED`, confirmed room discovery lists it with `hasAccess:false` (not hidden), confirmed the 6 legacy rooms are `is_active=false`/`program_id=null` in the DB with zero fabricated associations. |
| 6 | Frontend: billing/programs/player/admin pages, router changes, locale key restructuring | **Done, verified** — completed concurrently (by ChatGPT, per this handover) while this session worked the backend slices. Frontend build/typecheck/locale-parity/59 tests all pass. |
| 7 | Cross-program negative-test sweep, migration verification, manual acceptance journey, `docs/TASKS.md` checkbox sync | **Done, verified** — see below. |

**The entire migration (P3.33–P3.45) is now complete.** Gaps found and closed directly in this session (not by any background agent):
- **`ProgramPlayerPage` had no way to exit back to the app** — its route sits outside the main layout (no header/nav), and only had prev/next content navigation. Fixed: an exit link was added to both the mobile top bar and desktop sidebar header (`content:exitProgram` locale key, `en`/`ro`).
- **P3.36's admin commercial UI was actually missing** (docs/TASKS.md had it marked "deferred" but nothing existed) — added a create-offer form and an inline price-update form to `AdminBillingListPage.tsx`, wired to the already-existing backend routes. Live-verified end-to-end.
- **P3.41.c had zero test coverage for `GetVideoPlaybackHandler`'s access gate** — added `Video_playback_requires_access_to_the_owning_program_not_just_any_program` to `ContentFlowTests.cs`.
- **A real bug**: 11 of 12 concurrent duplicate webhook deliveries returned HTTP 500 instead of an idempotent success. Root cause: `GrantOrReactivateEntitlementAsync` did an early nested `SaveChangesAsync` that contradicted the class's own "same transaction" doc comment — when it failed and was caught, the outer `HandleAsync` re-attempted saving the same already-tracked `WebhookEvent`/`Payment`/`Invoice` entities from the aborted transaction, which then failed a *second* time on the `WebhookEvent.ProviderEventId` unique constraint, uncaught. Fixed in `src/Modules/Billing/Application/UseCases/ProcessProviderEventHandler.cs`: the whole unit of work now saves exactly once, with a single outer `catch (DbUpdateException)` that distinguishes a same-event race (safe no-op) from a same-entitlement-different-event race (retries once after dropping only the conflicting insert, so no data is silently lost). Re-verified twice after the fix: 12/12 and 12/12 concurrent requests returned 204 with zero server errors.
- **Empty-database full migration chain re-verified** — spun up a disposable Postgres 16 container, applied all 12 migrations from scratch, confirmed the resulting 50-table schema matches expectations exactly (all new `program_*`/`chat_rooms`/`event_programs` tables present, zero leftover old-model tables).
- **Consolidated Chat/Events buy-A/deny-B/refund-revoke journey** run live against the real API/DB — all 12 assertions passed (see `docs/TASKS.md` P3.45.d for the full list).

`docs/TASKS.md` section 3.G is now fully synced against actual verified reality — every subtask is checked with an evidence note except the one deliberately-accepted P3.34.b data-loss deviation.

### First thing to do

1. `cd c:\Proiecte\B-united && git status` — see what's actually uncommitted. **Nothing in this migration has been committed to git** — it's all sitting in the working tree. Do not run destructive git commands.
2. `dotnet build BUnited.sln` — must be 0 errors. As of this update: 0 errors, 0 warnings.
3. `dotnet test BUnited.sln` — as of this update: **307 passed, 0 failed** across 11 test projects.
4. `cd frontend && npx tsc -b && npm run build && npm test -- --run && node scripts/check-locale-parity.mjs` — as of this update: all clean, 59 frontend tests passing.
5. Check whether the local API is running: `curl http://localhost:5080/health/live` (expect `200`). If backend files changed since it was last started, restart it (see "Running the app locally" below) — `dotnet run` does not hot-reload.
6. Since the migration is complete, the next natural step is deciding whether/when to commit this work — nothing has been committed yet, and that decision belongs to the user, not to whichever agent picks this up next.

## Key architecture decisions already made (do not re-litigate)

- **No outbox infrastructure.** `ProgramEntitlement` granting happens synchronously in the same DB transaction as marking a `Purchase` succeeded — this matches the only existing precedent in the codebase (`ProcessProviderEventHandler`). Don't introduce a message queue or outbox table.
- **Existing demo subscription data was disposable.** The Slice 1 migration dropped `Plans`/`PlanPrices`/`Subscriptions`/`SubscriptionPeriods`/old `Entitlements` outright — confirmed with the user, no real subscriber data existed. This is already done; don't second-guess it.
- **Legacy chat rooms are deactivated, not fabricated-mapped.** When `ChatRoom` becomes a program-owned entity (Slice 5), the 6 old fixed rooms (`General/Psychology/Sport/Nutrition/Business/FinancialEducation`) get `IsActive=false` with message history preserved — admins create fresh program-scoped rooms afterward. Confirmed with the user; don't invent fake program associations for them.
- **Cross-module contract pattern**: every module boundary in this codebase uses a `Contracts` project (interface, no implementation) + `Infrastructure/CrossModule/` (the implementation), read-only, `AsNoTracking`. Examples already built: `Identity.Contracts.IUserLookup`, `Content.Contracts.IProgramLookup`, `Progress.Contracts.IContentItemProgramLookup`, `Billing.Contracts.IProgramOfferLookup`. Follow this exact pattern for any new cross-module read. **A module must never reference another module's Domain or Infrastructure layer directly**, and `ProgramId` columns crossing module boundaries are always plain `Guid` with no database foreign key (Billing/Questionnaires/Events/Chat all reference Content's `Program` this way).
- **`IProgramAccessContext`** (`src/BuildingBlocks/Application/Access/IProgramAccessContext.cs`) — `HasProgramAccessAsync(userId, programId, ct)` / `RequireProgramAccessAsync(userId, programId, ct)`, backed by `BillingProgramAccessContext` querying `ProgramEntitlement`. Stable error codes in the same folder's `ProgramAccessErrorCodes`: `PROGRAM_ACCESS_REQUIRED`, `PROGRAM_ALREADY_OWNED`. This is the one contract every consumer module (Content, Progress, Questionnaires, Events, Chat) calls before serving protected program-scoped data.
- **Expert/moderator/admin RBAC stays independent of commercial entitlement.** An Expert reviewing questionnaire submissions, or a moderator acting on chat messages, does **not** need to have purchased the program — administrators also have **no implicit access** to sensitive data (questionnaire answers, guidance) purely by virtue of being admins. These are separate, deliberate authorization paths that must never be conflated with buyer entitlement. Keep this invariant in any new code.
- **Catalogue-open, detail-protected pattern**: published program/questionnaire lists stay browsable to any authenticated (sometimes anonymous) user; the protected payload (content body, media, guidance, chat history) is what actually gets gated. Follow this same shape for Slice 6's frontend paywall UX and for the remaining Chat room-discovery decision in Slice 5.

## Running the app locally

- Backend: `cd src/Api && dotnet run --no-launch-profile --urls http://localhost:5080` (set `ASPNETCORE_ENVIRONMENT=Development` first). Health check: `GET /health/live`. **Restart it after any backend rebuild** — no hot reload.
- Local Postgres connection string lives in the repo's `.env` (`ConnectionStrings__Default`) — gitignored, already provisioned locally on this machine. Database name `bunited`.
- Frontend: `cd frontend && npm run dev` (Vite). Typecheck: `npx tsc -b`. Locale parity check: `node scripts/check-locale-parity.mjs` — must pass after any locale JSON edit (ro/en key parity is mandatory in the same change).
- **Admin login**: `admin@bunited.local` / `Admin1234!` (password was reset this session at the user's request; the account itself pre-existed with Administrator + Expert roles — do not delete it).

## What's left

Nothing — the migration (P3.33–P3.45) is complete as of this update. See the status table and gap list near the top of this document for what was built and verified.

## Mandatory rules to keep following (from `docs/DEVELOPMENT_INSTRUCTIONS.md`, non-exhaustive highlights)

- Money as `decimal` with explicit currency; timestamps in UTC.
- EF entities are never returned as API DTOs directly.
- Every behavior change needs the smallest effective automated regression test, including negative/cross-user/cross-program cases for protected resources.
- No destructive git commands without explicit authorization. Nothing in this migration has been committed — check with the user before committing/pushing anything.
- Do not claim a task complete without an actual `dotnet build` + `dotnet test` run (and live curl verification where practical) — "the code looks right" is not a completion criterion in this repo.
- ro/en locale key parity is mandatory in the same change as any UI text change.

## Continuation update (2026-08-09)

Slices 5 and 6 are now implemented and verified. Slice 5 passed the full backend suite and received the missing forward `AssociateEventsWithPrograms` migration; the migration is intentionally idempotent because the local development database already contained the join table without a matching migration-history row. Slice 6 now uses purchase/offer/entitlement terminology and routes, includes localized catalogue/detail/player paywalls, My Purchases/owned-programs/invoices, admin purchases/offers, program-owned Chat room IDs/locked discovery, and Event program-association editing. Verification: 0-warning solution build, 306 backend tests, frontend production build, 59 frontend tests, and ro/en locale parity.

Slice 7 remains partially open. Live health and buy-A/play-A/deny-B/refund-A/revoke-A were verified, including stable `PROGRAM_ACCESS_REQUIRED` responses. A truly empty-database migration run and live program-scoped Chat/Event plus historical-data-preservation journey remain unverified and are recorded under P3.45.d.

## Final update (2026-08-09, later same day)

Slice 7 is now fully closed. Three items closed this pass: (1) empty-database full migration chain re-verified from a disposable Postgres 16 container — all 12 migrations applied cleanly, resulting 50-table schema matches expectations exactly; (2) true concurrent-delivery testing found and fixed a real bug — see the gap list near the top of this document for the full root-cause account; (3) a consolidated Chat/Events buy-A/deny-B/refund-revoke journey run live against the real API/DB, all 12 assertions passed. `docs/TASKS.md` P3.33–P3.45 is now fully synced — every subtask checked with an evidence note except the one deliberately-accepted P3.34.b data-loss deviation. Final state: 307 backend tests / 59 frontend tests, 0 build warnings/errors, backend confirmed healthy and running with all fixes applied.

## If something looks inconsistent

Prefer investigating over assuming. The background agents that built Slices 1-4 each independently re-read the actual current code before writing anything (rather than trusting plan-file text verbatim), verified against a real Postgres instance, and reported residual risks/judgment calls honestly rather than glossing over them — their individual completion reports (not preserved outside the original session, but referenced by their file diffs and the `docs/TASKS.md` notes they left) are the ground truth for exactly what was built and why. When in doubt, read the actual code, not this summary.
