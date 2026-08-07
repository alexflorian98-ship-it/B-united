# Module: Progress

ContentProgress and SectionProgress tracking, derived program-level progress calculation.

## Layers

- `Domain` — entities, value objects, domain events, invariants owned by this module.
- `Application` — use cases / handlers, validators, DTOs consumed by the Api layer.
- `Infrastructure` — EF Core configurations, repositories, external integrations owned by this module.
- `Api` — controllers/endpoints exposed under `/api/v1`.
- `Contracts` — public interfaces/DTOs other modules are allowed to depend on (no Infrastructure or Domain leakage).
- `Tests` — unit and integration tests for this module.

## Rules

- No other module may reference this module's `Domain` or `Infrastructure` layers directly.
- Cross-module interaction happens only through `Contracts`.
