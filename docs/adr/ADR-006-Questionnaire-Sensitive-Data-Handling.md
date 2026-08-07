# ADR-006: Questionnaire Sensitive Data Handling

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

Questionnaire submissions and guidance are treated as high-sensitivity data regardless of formal legal classification. Access is restricted to the submitting client and the authorized expert; administrators do not get automatic access. Content is excluded from logs, analytics and notifications, and reads are audited.

## Consequences

(To be documented alongside the related implementation phase.)
