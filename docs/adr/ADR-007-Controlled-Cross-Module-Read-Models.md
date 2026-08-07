# ADR-007: Controlled Cross-Module Read Models

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

Read-only administrative/dashboard projections (e.g. SubscriberAdminView) may join data owned by multiple modules directly, for simplicity. These read models must remain read-only, live in dedicated query code, and must never become a hidden business dependency or mutate another module's data.

## Consequences

(To be documented alongside the related implementation phase.)
