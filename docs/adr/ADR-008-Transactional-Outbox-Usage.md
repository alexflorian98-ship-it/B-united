# ADR-008: Transactional Outbox Usage

## Status

Accepted (revised 2026-08-10 — the original text scoped a subscription-era outbox that was never
built; this revision reflects what V1 actually implements).

## Context

`CLAUDE.md` states the outbox should be used "only for important cross-module events that require
retry or delivery guarantees." Phase 1 through Phase 6 delivered Identity, Content, Billing
(per-program purchases, not subscriptions — see ADR-003, ADR-010), Questionnaires, Events, and
Chat without introducing an outbox table or dispatcher anywhere in the codebase (`src/` has no
`Outbox`/`OutboxMessage` type). Every cross-module effect implemented so far — entitlement
activation on purchase, questionnaire submission notifications, event registration, guidance
publication — has been handled synchronously in-process, inside the same database transaction as
the triggering command, through Contracts-layer calls or direct in-process handlers.

The originally planned candidate list (`SubscriptionActivated`, `SubscriptionExpired`,
`PaymentFailed`, `QuestionnaireSubmitted`, `GuidancePublished`, `EventPublished`,
`EventRegistrationCreated`) referenced a recurring-subscription billing model that no longer
exists (ADR-003, ADR-010) and was never actually wired to an outbox.

## Decision

**V1 does not implement a transactional outbox.** Entitlement activation stays synchronous with
purchase/webhook processing, and all other cross-module notifications currently in scope are
delivered the same way: synchronously, in-process, within the originating transaction. No new
outbox infrastructure is introduced as part of Milestone A (Demo MVP) — this is a locked decision
(see `docs/IMPLEMENTATION_PLAN.md` §3), not an oversight to fix opportunistically.

An outbox remains the right tool if a future requirement needs cross-module delivery that must
survive a process crash or an unreachable external system after the local transaction commits —
for example, real transactional email delivery (Phase 8 / Slice B4) or a real payment provider's
webhook-triggered side effects that must not be lost on failure. That decision should be made
explicitly, scoped to the specific event(s) that need it, when that Phase 8 slice is designed —
not spent speculatively in Milestone A.

## Consequences

- No outbox table, dispatcher, or background relay exists or needs migrating in V1.
- Cross-module reliability for synchronous in-process calls depends on the surrounding database
  transaction: if the transaction commits, the effect happened; there is no separate retry path
  for a failure after commit, because none of today's cross-module effects run outside that
  transaction.
- Introducing an outbox later (Phase 8+) requires its own ADR update naming the specific events it
  will carry, since the original candidate list here is no longer accurate.
