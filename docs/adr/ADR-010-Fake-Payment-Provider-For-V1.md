# ADR-010: Fake Payment Provider for V1 Demo Billing

## Status

Accepted (revised 2026-08-10 to reflect the per-program purchase model — see ADR-003 and the
program-commerce migration; the original text described a recurring-subscription model that has
since been replaced everywhere in code).

## Context

`docs/PROMPT.md` §17 names Stripe as the initial payment provider and requires a real
Checkout Session → Stripe → webhook → `Purchase` → `ProgramEntitlement` pipeline with signature
verification. As with ADR-005's video-provider decision, no Stripe (or any other payment
provider) account/credentials exist in this environment. Building a real Stripe adapter would
be code-complete but never live-verified against the real service — inconsistent with this
project's working method of live-testing every slice against a real backend/database.

Unlike the video-provider case, billing is explicitly called out (docs/PROMPT.md §68) as the
**highest-risk area requiring the most rigorous testing** — webhook idempotency, out-of-order
delivery, refunds, chargebacks, and duplicate/retry delivery. Shipping this behind an untested
real-provider stub would defeat the purpose of building it now.

`docs/TASKS.md`'s Phase 3 was restructured (prior to this implementation pass) from "Billing and
access" (real Stripe) into **"Simulated billing and real local access"**: the full purchase
and entitlement lifecycle must be demonstrable and rigorously testable without external
credentials, with the real provider integration explicitly deferred to a separate Phase 8
("Real integrations and production operations"). Billing later migrated from a recurring
subscription model to one-time per-program purchases (ADR-003); the fake-provider decision below
still holds, only the entity/event vocabulary changed.

## Decision

**V1 implements a `FakePaymentProvider` behind an `IPaymentProvider` abstraction, not a real
Stripe adapter.** Checkout is a local, deterministic simulation:

- A "checkout session" is created locally (no external HTTP call) for a specific `Purchase` of
  one program and immediately resolves to a configurable outcome (success / decline /
  provider-error / timeout), selectable by the caller for test-matrix purposes and defaulting to
  success for the normal demo flow.
- Provider **events** (the fake equivalents of Stripe webhooks — `ProviderEventType.PaymentSucceeded`,
  `PaymentFailed`, `PaymentRefunded`, `PaymentChargedBack`) are generated **server-side only**,
  carrying a local demo signature/secret, and POSTed to the same webhook-processing endpoint a
  real provider's webhook would hit. This endpoint enforces the same idempotency
  (`WebhookEvent.ProviderEventId` uniqueness), out-of-order-safety, and audit-trail requirements
  a real Stripe webhook handler would need — the simulation exercises the *real* risk area (§68),
  not a shortcut around it.
- The demo event-submission endpoint **only accepts server-generated events with a valid local
  demo signature** and is **hard-disabled outside `Development`/`Demo` environments** (P3.32):
  the application fails to start in `Production` if `FakePaymentProvider` (or any other fake
  adapter — video, email, storage) is registered, so this can never accidentally ship live.
- `Purchase`/`Payment`/`Invoice`/`WebhookEvent`/`ProgramEntitlement` are all real, fully-modeled
  entities — nothing about the *data model* or the *state machine* is fake. `Purchase` moves
  through `Pending → Succeeded/Failed`, with `Refunded`/`Chargeback` as later transitions
  (`PurchaseStatus`); only the outbound HTTP call to an external payment processor is simulated.
- Admin/client UI includes explicit **demo-only controls** (mark paid, fail payment, refund) that
  drive the same fake-event pipeline a real Stripe test-mode webhook would, with a visible
  "simulated payment" notice so it is never mistaken for a real transaction.

## Consequences

- **The entire purchase/entitlement/access lifecycle is genuinely testable end-to-end** today,
  including the scenarios §68 calls highest-risk (idempotent/out-of-order webhook processing,
  refund, chargeback, duplicate provider event, retry after transient failure) — none of that had
  to wait for real credentials.
- **No real money ever moves in V1.** This is explicitly a demo/local-access system until Phase 8
  replaces `FakePaymentProvider` with a real Stripe (or other) adapter behind the same
  `IPaymentProvider` interface — swapping providers means writing a new adapter, not touching the
  domain model, state-machine logic, or `IProgramAccessContext` consumers.
- **P3.32's production safety gate is load-bearing, not optional.** Because the fake provider is
  real, reachable application code (not a `#if DEBUG` block), the only thing preventing it from
  accidentally being live in production is the explicit startup check — this must ship in the
  same slice as the fake provider itself, not as a follow-up.
- Revisit before accepting real payments: Phase 8 (`docs/TASKS.md` P8.01–P8.05) covers selecting
  and integrating the real provider, real webhook signature verification against that provider's
  actual signing scheme, and a provider sandbox + controlled production smoke test.
