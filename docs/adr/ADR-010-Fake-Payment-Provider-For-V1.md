# ADR-010: Fake Payment Provider for V1 Demo Billing

## Status

Accepted

## Context

`docs/PROMPT.md` §17 names Stripe as the initial payment provider and requires a real
Checkout Session → Stripe → webhook → `Subscription` → `Entitlement` pipeline with signature
verification. As with ADR-005's video-provider decision, no Stripe (or any other payment
provider) account/credentials exist in this environment. Building a real Stripe adapter would
be code-complete but never live-verified against the real service — inconsistent with this
project's working method of live-testing every slice against a real backend/database.

Unlike the video-provider case, billing is explicitly called out (docs/PROMPT.md §68) as the
**highest-risk area requiring the most rigorous testing** — webhook idempotency, out-of-order
delivery, grace-period boundaries, cancellation/expiration/re-subscription. Shipping this
behind an untested real-provider stub would defeat the purpose of building it now.

`docs/TASKS.md`'s Phase 3 was restructured (prior to this implementation pass) from "Billing and
access" (real Stripe) into **"Simulated billing and real local access"**: the full subscription
and entitlement lifecycle must be demonstrable and rigorously testable without external
credentials, with the real provider integration explicitly deferred to a separate Phase 8
("Real integrations and production operations").

## Decision

**V1 implements a `FakePaymentProvider` behind an `IPaymentProvider` abstraction, not a real
Stripe adapter.** Checkout is a local, deterministic simulation:

- A "checkout session" is created locally (no external HTTP call) and immediately resolves to a
  configurable outcome (success / decline / provider-error / timeout), selectable by the caller
  for test-matrix purposes and defaulting to success for the normal demo flow.
- Provider **events** (the fake equivalents of Stripe webhooks — `subscription.activated`,
  `payment.succeeded`, `payment.failed`, `subscription.canceled`, `subscription.expired`,
  `subscription.renewed`) are generated **server-side only**, carrying a local demo
  signature/secret, and POSTed to the same webhook-processing endpoint a real provider's
  webhook would hit. This endpoint enforces the same idempotency (`WebhookEvent.ProviderEventId`
  uniqueness), out-of-order-safety, and audit-trail requirements a real Stripe webhook handler
  would need — the simulation exercises the *real* risk area (§68), not a shortcut around it.
- The demo event-submission endpoint **only accepts server-generated events with a valid local
  demo signature** and is **hard-disabled outside `Development`/`Demo` environments** (P3.32):
  the application fails to start in `Production` if `FakePaymentProvider` (or any other fake
  adapter — video, email, storage) is registered, so this can never accidentally ship live.
- `Subscription`/`SubscriptionPeriod`/`Payment`/`Invoice`/`WebhookEvent`/`Entitlement` are all
  real, fully-modeled entities per §15 — nothing about the *data model* or the *state machine* is
  fake. Only the outbound HTTP call to an external payment processor is simulated.
- Admin/client UI includes explicit **demo-only controls** (renew, fail payment, cancel, expire)
  that drive the same fake-event pipeline a real Stripe test-mode webhook would, with a visible
  "simulated payment" notice so it is never mistaken for a real transaction.

## Consequences

- **The entire subscription/entitlement/access lifecycle is genuinely testable end-to-end**
  today, including the scenarios §68 calls highest-risk (idempotent/out-of-order webhook
  processing, grace-period boundaries, cancellation-until-period-end, re-subscription) — none of
  that had to wait for real credentials.
- **No real money ever moves in V1.** This is explicitly a demo/local-access system until Phase 8
  replaces `FakePaymentProvider` with a real Stripe (or other) adapter behind the same
  `IPaymentProvider` interface — swapping providers means writing a new adapter, not touching the
  domain model, state-machine logic, or `IAccessContext` consumers.
- **P3.32's production safety gate is load-bearing, not optional.** Because the fake provider is
  real, reachable application code (not a `#if DEBUG` block), the only thing preventing it from
  accidentally being live in production is the explicit startup check — this must ship in the
  same slice as the fake provider itself, not as a follow-up.
- Revisit before accepting real payments: Phase 8 (`docs/TASKS.md` P8.01–P8.05) covers selecting
  and integrating the real provider, real webhook signature verification against that provider's
  actual signing scheme, and a provider sandbox + controlled production smoke test.
