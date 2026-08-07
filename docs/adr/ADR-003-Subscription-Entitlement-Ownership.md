# ADR-003: Subscription Entitlement Ownership

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

Billing owns subscription state and the PlatformAccess entitlement exclusively. Other modules consume access decisions only through IAccessContext (HasPlatformAccessAsync / RequirePlatformAccessAsync). Content and other modules never create or own entitlement records.

## Consequences

(To be documented alongside the related implementation phase.)
