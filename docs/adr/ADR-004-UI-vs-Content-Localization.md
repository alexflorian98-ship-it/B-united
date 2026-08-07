# ADR-004: UI vs Content Localization

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

UI labels live in frontend i18next locale files (src/locales). Business content (programs, sections, content items, events, questionnaires) is localized through dedicated *Translation database tables with a default language and fallback + translationFallbackUsed flag. These are deliberately separate systems; no generic DB-driven UI localization engine is built in V1.

## Consequences

(To be documented alongside the related implementation phase.)
