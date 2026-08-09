# ADR-005: Video Hosting Provider Abstraction

## Status

Accepted (revised during Phase 2 implementation, 2026-08-08 — see "V1 decision" below).

## Context

`docs/PROMPT.md` §18–22 requires video playback to go through "signed/short-lived playback URLs issued only after PlatformAccess authorization succeeds," and suggested a dedicated provider (Mux / Cloudflare Stream / Vimeo) behind an abstraction so the application database only stores `MediaAsset` metadata and provider identifiers, never raw video files.

At the point Phase 2 implementation started, no provider account/credentials existed for any of Mux, Cloudflare Stream, or Vimeo — `.env`'s `VideoProvider__ApiKey`/`ApiSecret` were empty. Building a real adapter against any of them would have been code-complete but never live-verified, unlike every other slice delivered this session (which was live-tested against a real Postgres + running Api).

## Decision

**V1 uses YouTube (unlisted videos) as the video "provider," not Mux/Cloudflare/Vimeo.** The expert pastes an existing YouTube video URL or ID when authoring a Video content item; the backend validates/normalizes it into a `MediaAsset` (`Provider = "youtube"`, `ProviderAssetId` = the 11-character YouTube video ID) and marks it `Ready` immediately — there is no upload/transcode/webhook pipeline, because the video already exists on YouTube's infrastructure by the time it's referenced here.

The `IVideoProvider` abstraction (docs/PROMPT.md §18–22's intent) is still implemented, with a `YouTubeVideoProvider` as its only concrete adapter for now. Nothing outside `Content.Infrastructure`/`Content.Application`'s use of that interface knows the provider is YouTube specifically — swapping to Mux/Cloudflare Stream later (see "Consequences") means writing a new adapter, not touching `MediaAsset`, the domain model, the API contracts, or the frontend player's calling code.

## Consequences

- **Accepted, documented gap: no real access enforcement on the video URL once issued.** The Api still requires authentication and the `IProgramAccessContext` (`UserId` + `ProgramId`) entitlement check before it will *hand back* a playback URL/embed reference at all — a logged-out or unentitled caller gets nothing from the Api. But once an entitled client has the URL, it is a normal `youtube.com/embed/{id}` reference: not short-lived, not cryptographically tied to that specific viewer or their entitlement state, and functions for anyone who obtains it (entitled or not, even after the original viewer's access is revoked). This is a real trade-off against the letter of docs/PROMPT.md §18–22's "signed/short-lived playback URL" requirement, made deliberately for V1 development speed and zero cost/credential dependency — not something to treat as already solved.
- **Revisit before a paying public launch.** Once real purchase revenue is at stake, re-evaluate Mux/Cloudflare Stream (both support the originally-specified signed/short-lived/DRM-capable playback model) before relying on YouTube's access characteristics in production with paying clients.
- No processing-status pipeline exists for V1 (`MediaProcessingStatus.Processing`/`Failed` are effectively unused — YouTube assets go straight to `Ready`). A provider that requires real upload/transcode (Mux, Cloudflare Stream) would need that pipeline built when it's introduced — it's modeled in the schema (`MediaProcessingStatus` enum) but has no working implementation behind it yet.
- No video duration is captured automatically (would require the YouTube Data API and an API key, which V1 doesn't use to keep this credential-free) — `MediaAsset.DurationSeconds` stays null for V1. Thumbnail *is* available without any credential (YouTube's well-known `img.youtube.com/vi/{id}/hqdefault.jpg` pattern), so that field is populated.
