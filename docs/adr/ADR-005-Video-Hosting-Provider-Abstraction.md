# ADR-005: Video Hosting Provider Abstraction

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

Video is hosted by a dedicated provider (Mux / Cloudflare Stream / Vimeo) behind a provider abstraction. The application database stores only MediaAsset metadata and provider identifiers. Playback URLs are short-lived and issued only after PlatformAccess authorization succeeds.

## Consequences

(To be documented alongside the related implementation phase.)
