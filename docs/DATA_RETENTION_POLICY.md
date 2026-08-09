# B-United Data Retention Policy

This document is the authoritative retention policy referenced by docs/PROMPT.md §66 ("Data
export and deletion") and docs/DEVELOPMENT_INSTRUCTIONS.md §6. It defines, per data category,
what happens when a client deletes their own account (`POST /api/v1/profile/delete`): hard
delete, anonymize, or retain — and why. It is implemented by the `IUserDataEraser` /
`IUserDataExporter` cross-module contracts in `src/BuildingBlocks/Application/DataRights/`.

Account deletion is **self-service and irreversible**. There is no separate administrator-driven
deletion workflow in V1; administrators have no implicit access to trigger or reverse it.

## Summary table

| Category | Entities | Action | Reason |
|---|---|---|---|
| Identity — account | `User` | **Anonymize** (row kept) | See "Why the `User` row is anonymized, not deleted" below. |
| Identity — consent history | `UserConsent` | **Retain, unmodified** | Compliance record; already `DeleteBehavior.Restrict` at the database level — the schema itself already refuses to let a `User` deletion cascade over it. |
| Identity — preferences | `UserPreference` | **Hard delete** | No retention value once the account is gone. |
| Identity — tokens | `RefreshToken`, `EmailVerificationToken`, `PasswordResetToken` | **Hard delete / revoke** | Security material with no value after account closure; revoking immediately also ends all active sessions. |
| Identity — role assignments | `UserRole` | **Retained (inert)** | Login is blocked via the password/lockout scramble below regardless of role assignment; removing role rows adds risk (FK/audit surface) for no security benefit. Documented as a deliberate no-op. |
| Progress | `ContentProgress`, `SectionProgress` | **Hard delete** | The user's own learning history, no third-party or legal retention interest. Not referenced by any retained record (Audit logs never join against Progress rows). |
| Questionnaires — submissions/answers | `QuestionnaireSubmission`, `QuestionnaireAnswer` | **Hard delete** | High-sensitivity personal data (CLAUDE.md: "no implicit administrator access"); no legal retention reason identified for V1. Cascades to `GuidanceResponse`/`GuidanceFollowUp` at the database level. |
| Questionnaires — guidance | `GuidanceResponse`, `GuidanceFollowUp` | **Hard delete (cascades with the submission)** | See "Guidance authored by the Expert" below. |
| Billing | `Purchase`, `Payment`, `Invoice`, `WebhookEvent`, `PaymentCustomer`, `ProgramEntitlement` | **Retain, unmodified, indefinitely** | Financial/audit record-keeping (§66: "legally required billing retention"). `UserId` becomes an orphaned opaque reference — this already matches the existing modular-monolith convention (no FK from Billing to `User`; see "Cross-module reference shape" below). |
| Events | `EventRegistration` | **Cancel (soft), not hard delete** | Uses the entity's existing `Cancel()` domain transition rather than deleting rows, so event capacity/attendance history stays internally consistent. Low sensitivity, no legal retention need. |
| Chat — messages | `Message` | **Retain verbatim; author reference orphaned** | §66: "do not destroy conversation continuity... replace a deleted user's identity with an anonymized representation." `Body`, `CreatedAt`, `IsPinned`, ordering are untouched. |
| Chat — moderation/read state | `Mute`, `Report`, `ChatReadState` | **Hard delete** | Operational/moderation bookkeeping tied to the now-deleted account, no independent retention value. |
| Audit | `AuditLog` | **Retain, unmodified, indefinitely** | Audit exists specifically to prove what happened and by whom; deleting audit history on account deletion would defeat its purpose. `ActorUserId` was already an opaque `Guid?` with no FK to `User` (CLAUDE.md/AuditLog's own doc comment), so it survives untouched as an orphaned reference. Not included in the user's own data export (it is an operational security record, not personal data the user "owns" in the export sense). |
| Content | `Program`, `Section`, `ContentItem`, `MediaAsset` | **N/A — no user data** | Instructor-authored, not user-owned (CLAUDE.md). Nothing to erase or export here. |
| Files | — | **N/A** | The Files module is still an empty scaffold with no real implementation in this codebase; there are no attachments to export or delete. |

## Why the `User` row is anonymized, not deleted

The instinctive V1 design would hard-delete the `User` row entirely. That is not possible here:
`UserConsentConfiguration` configures `UserConsent.UserId → User.Id` with
`DeleteBehavior.Restrict`, specifically *because* "consent history is a compliance record and
must never be silently lost as a side effect of an unrelated user-deletion cascade" (see that
file's own comment, written before this feature existed). As long as a `UserConsent` row for the
account exists — which it always will, consent being recorded at registration — the database
itself refuses to let the `User` row be deleted. Anonymizing `User` in place is therefore not a
preference, it is the only option compatible with the schema as already designed, and it also
happens to be the correct GDPR-style outcome (retain the compliance record, sever the identity).

Anonymization (`User.AnonymizeForDeletion`):
- `Email`/`NormalizedEmail` are replaced with a per-account unique, unguessable placeholder
  (`deleted-{userId:N}@deleted.bunited.local`) — unique because `NormalizedEmail` has a database
  unique index, so two deletions can never collide.
- `PasswordHash` is replaced with the hash of a random, discarded value — nobody can ever
  authenticate as this account again, including the former owner.
- `LockoutEndUtc` is set far in the future as defense-in-depth alongside the password scramble.
- `IsActive` is set to `false`.
- `LoginHandler` additionally rejects `IsActive == false` explicitly with a distinct
  `ACCOUNT_DELETED` error, rather than relying solely on the lockout/password path, so a deleted
  account never falls back into the "account temporarily locked" story with vague retry
  semantics.

No new column is added for "when was this deleted" — the deletion audit log entry
(`user.account_deleted`) already carries that timestamp as the authoritative record, and adding a
redundant column was judged unnecessary scope for V1.

## Cross-module reference shape

Every other module already stores `UserId` as an opaque `Guid` with **no foreign key** to
`Identity.User` — this is the same convention already established for `ProgramId` references
into Content (verified directly in each module's EF configuration: `PurchaseConfiguration`,
`ProgramEntitlementConfiguration`, `ContentProgress`'s own doc comment, `EventRegistration`,
`Message`). This is what makes Billing retention, Progress/Questionnaires/Events erasure, and
Chat's message-row survival all safe independently of what happens to the `User` row: nothing in
the database enforces referential integrity across those boundaries, by design (CLAUDE.md:
"Never reference another module's Domain or Infrastructure layer").

## Guidance authored by the Expert

`GuidanceResponse.Body` is written by the Expert, not the client — but it exists only as a reply
to that specific client's `QuestionnaireSubmission`/`QuestionnaireAnswer` set, which is being
hard-deleted as high-sensitivity personal data belonging to the client. A guidance response with
no surviving submission to attach to is not independently meaningful content (it has no display
context, no recipient, and — per docs/PROMPT.md §25–28 — guidance is explicitly "not direct
messaging" with any life of its own outside a submission). It is therefore allowed to cascade-
delete with the submission (`GuidanceResponseConfiguration`'s existing
`OnDelete(DeleteBehavior.Cascade)` on `QuestionnaireSubmissionId`, unchanged by this feature). The
Expert's own record of having done the work is not separately preserved in V1; this is a
deliberate scope decision, not an oversight — expert workload/performance is measured from
`QuestionnaireSubmission`'s own operational timestamps in aggregate reporting, not from retained
guidance text, and guidance text is treated as high-sensitivity per the client, so keeping a copy
tied to a deleted client's private data would work against, not for, the account-deletion intent.

## Events — soft cancel instead of hard delete

`EventRegistration` has no `Delete` domain method, only `Cancel(DateTime utcNow)`. Reusing that
existing transition (rather than adding a new hard-delete path) keeps registration/attendance
counts internally consistent with how cancellation already works elsewhere in the module.
**Known limitation:** canceling a `Waitlisted`/`Registered` registration this way does not itself
trigger waitlist promotion for the next person in line — that already happens through the
existing Events job/handler path on the ordinary cancellation flow, and account deletion does not
separately invoke it. Given the low likelihood of a waitlisted deletion colliding with an
imminent event in V1's expected volumes, this is accepted as a residual risk rather than solved
in this slice; an administrator can manually re-run the existing promotion path if needed.

## What is included in the "export my data" JSON archive

`GET /api/v1/profile/export` aggregates every `IUserDataExporter` registered across modules,
scoped strictly to the caller's own `UserId` (never an admin-supplied identifier — matching the
existing questionnaire-export precedent of "no implicit administrator access"):

- **identity** — profile (email, timezone, preferred language, notification preference) and
  consent history.
- **progress** — content/section progress rows.
- **questionnaires** — full submission/answer/guidance history (reuses
  `ExportMyQuestionnaireDataHandler`, deliberately not gated by current program entitlement — a
  refunded program must not hide a client's own historical answers/guidance).
- **billing** — the caller's own purchases, payments, invoices, and entitlements. Never another
  user's, even for an administrator calling this endpoint as themselves.
- **events** — the caller's own event registrations.
- **chat** — the caller's own authored message history, capped at the 1,000 most recent messages
  per room-agnostic query to bound memory/response size (docs/DEVELOPMENT_INSTRUCTIONS.md §7's
  general principle of not loading unbounded history applies equally on the backend export path).

Audit logs and other users' Chat/Questionnaire data are never included. There are no file
attachments to include (Files module has no real implementation yet).
