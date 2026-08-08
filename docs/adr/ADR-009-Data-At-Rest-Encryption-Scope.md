# ADR-009: Data-at-Rest Encryption Scope for Sensitive Data

## Status

Accepted

## Context

The Phase 0 architecture draft specified "encryption at rest where feasible" for
questionnaire submissions and guidance text without defining what "feasible" means
operationally, which module owns it, or which migration introduces it. The
mandatory architecture review (§75, see [ARCHITECTURE.md](../ARCHITECTURE.md#12-architecture-review-§75))
flagged this as review item R3: an under-specified requirement risked blocking
Phase 4 (Questionnaires) delivery while the team debated column-level application
encryption, key management, and query-ability trade-offs with no legal
classification (§35) yet confirmed.

## Decision

For V1, rely on the hosting provider's disk-level encryption at rest plus TLS in
transit as the baseline protection for questionnaire submissions and guidance
text. Do not implement column-level application encryption in Phase 4. Instead,
enforce strict access control (P4.15) and metadata-only audit logging (P4.17) on
`Questionnaires` module data, consistent with [ADR-006](ADR-006-Questionnaire-Sensitive-Data-Handling.md).

Column-level application encryption remains an explicitly out-of-scope follow-up
for V1. It is not silently dropped: it must be revisited once the legal
classification of questionnaire/guidance data (§35) is confirmed, and tracked as
a new backlog item at that time rather than assumed complete.

## Consequences

- Phase 4 is not blocked on encryption key-management design or a query-ability
  trade-off study.
- Sensitive-data protection for V1 depends on infrastructure-level guarantees
  (provider disk encryption, TLS) and strict access control/audit, not
  application-level ciphertext — this must be disclosed accurately in any
  security or compliance review of V1.
- If legal classification later requires column-level encryption, it will need
  its own migration, key-management design, and a follow-up ADR superseding this
  one; this is deferred work, not resolved work.
