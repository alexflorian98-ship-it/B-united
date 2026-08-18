# Stripe webhook security test specification

Status: **NOT APPLICABLE to the current V1 codebase** (no Stripe integration exists — see
[ADR-010](../adr/ADR-010-Fake-Payment-Provider-For-V1.md)) — **REQUIRED BEFORE STRIPE PRODUCTION**.

This document exists so that when Phase 8 (`docs/TASKS.md` P8.01–P8.05) replaces
`FakePaymentProvider` with a real Stripe adapter, the security invariants below are implemented
and automated-tested in the same change, not retrofitted afterward. Do not mark any item PASS
until it has been executed against a real Stripe test-mode webhook.

## Why NOT APPLICABLE today

V1's webhook-equivalent endpoint only accepts **server-generated** fake provider events carrying
a local demo signature, and the fake provider adapter is hard-disabled outside
`Development`/`Demo` (`VerifyNoDemoOnlyAdaptersInProduction`, P3.32). There is no HMAC/signature
verification against a real provider secret to test, because there is no real provider. What *is*
already implemented and tested against the fake provider — and will carry over unchanged to the
real integration, since the webhook-processing endpoint and idempotency logic are provider-agnostic
(`ProcessProviderEventHandler`, `WebhookEvent.ProviderEventId` uniqueness) — is covered in
`docs/E2E_AUDIT_RESULT.md`'s coverage matrix under "Billing invariants testable today":

- duplicate event delivery grants exactly one entitlement (`ProgramCommerceFlowTests.Duplicate_event_delivery_grants_a_single_entitlement`);
- concurrent duplicate delivery processes exactly once (`Concurrent_duplicate_event_delivery_processes_exactly_once`);
- out-of-order events do not regress state (`Out_of_order_event_does_not_regress_state`);
- refund/chargeback preserves purchase history rather than deleting it (`Refund_flips_status_and_revokes_access_without_deleting_history`);
- amount/currency are always server-derived from `ProgramPrice`, never client-suppliable (`Checkout_ignores_any_client_supplied_amount_and_always_uses_the_server_side_offer_price`);
- entitlement is scoped to the `(UserId, ProgramId)` pair, not either alone (`Entitlement_is_scoped_to_both_user_and_program_not_either_alone`).

## Required before Stripe production (test spec)

Implement `StripeWebhookSecurityTests` covering, at minimum, one test per row:

| # | Requirement | Test approach |
|---|---|---|
| 1 | Signature verification rejects a request with no `Stripe-Signature` header | POST the real webhook route with a valid JSON body and no signature header; expect 400/401, event NOT persisted, NOT processed. |
| 2 | Signature verification rejects a request with a tampered signature | POST with a signature computed over a different payload (or a random string); expect rejection, no state change. |
| 3 | Signature verification rejects a request with a signature from the wrong webhook secret | Sign the payload with a different (valid-format) secret; expect rejection. |
| 4 | Timestamp tolerance rejects a replayed old event | Construct a validly-signed request whose `Stripe-Signature` timestamp is older than Stripe's own tolerance window (default 5 minutes) using a real Stripe test fixture/replayed payload; expect rejection distinct from a fresh valid one. |
| 5 | A validly-signed, fresh event is accepted and processed | POST a genuine Stripe CLI (`stripe trigger`) or test-mode webhook payload against a locally running instance with `Stripe listen --forward-to`; expect 200 and the matching `Purchase`/`ProgramEntitlement` state change. |
| 6 | Duplicate delivery of the same real Stripe event ID is idempotent | Deliver the identical signed payload twice; expect exactly one entitlement/state change, second delivery a no-op 200. |
| 7 | Out-of-order real events do not regress state | Deliver `charge.refunded` before `checkout.session.completed` for the same object (Stripe's own retry/redelivery can do this); expect the final state to reflect the correct business outcome, not last-write-wins. |
| 8 | Webhook body is never logged raw | Send a payload containing a canary string in an ignored field; assert (per `scripts/security/Test-LogLeakage.ps1`'s pattern) the canary never appears in application logs. |
| 9 | Webhook processing failures do not leak Stripe secret/internal details in the response | Force a processing exception; assert the response uses the standard error contract (`code`/`messageKey`/`correlationId`), not a stack trace or the webhook secret. |
| 10 | The webhook endpoint is reachable without the app's own JWT auth (Stripe cannot present one) but is NOT reachable as a general-purpose unauthenticated entitlement-granting endpoint | Confirm the only way to reach `ProcessProviderEventHandler`'s success path is through signature verification — i.e. removing/blanking the signature is the actual access control, not `[AllowAnonymous]` alone. |

## Non-negotiable implementation requirements (not just tests)

- Use Stripe's own SDK signature verification (`Stripe.Net`'s `EventUtility.ConstructEvent`), not
  a hand-rolled HMAC comparison.
- Store the real webhook signing secret (`Stripe__WebhookSecret` in `.env.example`) as a genuine
  secret — never logged, never returned in any API response.
- Reuse the existing `WebhookEvent.ProviderEventId` unique-index idempotency mechanism; do not
  invent a second one for Stripe specifically.
- `VerifyNoDemoOnlyAdaptersInProduction` must be updated so `FakePaymentProvider` remains
  rejected in Production once the real adapter exists — do not silently drop that gate during the
  migration.

## What must NOT be done

- Do not claim any row above PASS based on unit tests against a fake/mocked Stripe SDK — signature
  verification must be tested against payloads the real Stripe library either accepts or rejects.
- Do not skip timestamp-tolerance testing — replay of an old, previously-valid signed payload is a
  realistic attack if tolerance is misconfigured or absent.
- Do not process an event before signature verification succeeds, even "just to log it."
