# ADR-003: Program Purchase and Entitlement Ownership

## Status

Accepted (revised on 2026-08-09; supersedes the previous global subscription decision)

## Context

B-United V1 sells programs separately. A client makes a one-time purchase and receives permanent access only to that program. The previous global recurring-subscription model granted a single `PlatformAccess` entitlement and could not represent this rule.

## Decision

Content owns `Program`. Billing owns `ProgramOffer`, `ProgramPrice`, `Purchase`, payment records and `ProgramEntitlement` exclusively. `ProgramOffer` references the Content-owned program by opaque `ProgramId` without a cross-module database foreign key.

Other modules consume access decisions only through `IProgramAccessContext` using both `UserId` and `ProgramId`. Content and other modules never create, revoke or query Billing entitlement rows directly.

A successful, validated and idempotently processed provider webhook grants a permanent entitlement. The browser never grants access. Refunds, chargebacks, fraud handling or audited administrative corrections may revoke access without deleting historical user data.

## Consequences

- Buying one program cannot unlock another program.
- Billing must expose contracts that answer program-scoped access questions without leaking its persistence model.
- Questionnaires, guidance, progress, chat and associated events must carry or resolve a `ProgramId` before enforcing access.
- The database enforces one entitlement per `(UserId, ProgramId)` and protects against duplicate successful fulfilment.
- Existing `Plan`, `Subscription`, `SubscriptionPeriod` and global `PlatformAccess` implementation must be migrated; it is not the target architecture.
- V1 deliberately excludes recurring subscriptions, trials, grace periods and automatic access expiration.
