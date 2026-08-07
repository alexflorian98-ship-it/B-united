# ADR-001: Modular Monolith

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

One deployable ASP.NET Core application and one PostgreSQL database, organized into modules with explicit boundaries (Domain/Application/Infrastructure/Api/Contracts per module). Rejected microservices, Kubernetes and service mesh as unnecessary complexity for the target scale (~2,000 subscribers, ~200 concurrent users).

## Consequences

(To be documented alongside the related implementation phase.)
