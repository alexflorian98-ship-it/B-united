# ADR-002: PostgreSQL

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

Single relational database for all modules. Chosen for maturity, JSONB support where needed, and to avoid database-per-module complexity and distributed transactions.

## Consequences

(To be documented alongside the related implementation phase.)
