# Upload security checklist

Status: **NOT APPLICABLE to the current V1 codebase.**

## Finding

A repo-wide search (`IFormFile`, `MultipartBodyLengthLimit`, `[FromForm]` file parameters) found
**zero matches anywhere in `src/`**. The `Files` module (`src/Modules/Files/`) is an empty
scaffold: `Domain`, `Application`, `Infrastructure`, `Api`, and `Contracts` layers exist only as
`.csproj` project files with no `.cs` source and no tests. Its `README.md` describes intended
scope ("object storage abstraction for uploaded assets not covered by the video provider — e.g.
avatars, documents") but nothing is implemented. No controller anywhere in the application accepts
a user-supplied file.

This means every item in the original task list (extension/MIME mismatch, executable content
renamed as an image, oversized/empty files, double extensions, path traversal in file names/keys,
Unicode/control characters, upload/download authorization, cross-user access, content-disposition
safety, SVG/script payloads, archive bombs, storage-key isolation, signed URL expiry/replay) has
**no code path to test today**. Building speculative test infrastructure or production
scaffolding for an endpoint that doesn't exist would violate
DEVELOPMENT_INSTRUCTIONS.md §2 ("MUST NOT add placeholders, fake success paths, dead code, unused
abstractions").

## Mandatory checklist before the first upload endpoint ships

This becomes a required part of that feature's own acceptance criteria (DEVELOPMENT_INSTRUCTIONS.md
§11's completion gate) — not optional follow-up work. Do not merge a first upload endpoint without
addressing every row.

| # | Control | Requirement |
|---|---|---|
| 1 | Authorization | Upload requires an authenticated, entitled caller; the storage key is derived server-side from the caller's identity, never trusted from the client. |
| 2 | Size limit | Enforce both `MultipartBodyLengthLimit`/`Kestrel` request body size limits and an explicit application-level max size per upload type, rejected before the full body is buffered. |
| 3 | Empty file | Reject zero-byte uploads explicitly with a clear error code, not a silent empty object. |
| 4 | Extension/MIME validation | Validate the actual file content (magic-byte/signature sniffing, e.g. via a library, not the client-supplied `Content-Type` header or filename extension alone) against an explicit allow-list per feature (e.g. avatars: image types only). |
| 5 | Executable content disguised as an allowed type | The magic-byte check in #4 must reject a renamed executable/script even if its extension/declared MIME type matches an allowed type. |
| 6 | Double extensions | Reject filenames with multiple/stacked extensions (`.jpg.exe`) rather than trusting the last segment alone. |
| 7 | Filename/storage-key sanitization | Never use the client-supplied filename as a storage key or filesystem path component. Generate the key server-side (e.g. a GUID); if the original filename must be retained for display, store it as metadata, sanitized against path traversal (`../`), Unicode homoglyphs, and control characters. |
| 8 | Storage-key isolation | Storage keys/paths must be namespaced per owning user/resource so no key-guessing or off-by-one path can reach another user's object; verify with a cross-user IDOR test (same pattern as `BillingCrossUserAccessTests`/`QuestionnaireCrossUserAccessTests`). |
| 9 | Content-Disposition | Serve user-uploaded content with `Content-Disposition: attachment` (or a strict `Content-Security-Policy: sandbox` if inline display is required) and never as `text/html`/`image/svg+xml` rendered inline from an untrusted upload, to prevent stored XSS via the file itself. |
| 10 | SVG/script payloads | If SVG uploads are ever allowed, sanitize them (strip `<script>`, event-handler attributes, `xlink:href` to external resources) server-side before storage — SVG is executable content, not a plain image format. |
| 11 | Archive bombs | If archive uploads are ever accepted, enforce a decompressed-size limit and a maximum nesting depth before extracting, and extract into an isolated, size-bounded location. |
| 12 | Signed URL expiry | Any download URL issued to the client must be short-lived and single-purpose (mirrors the existing video-playback pattern, ADR-005), never a permanent public link. |
| 13 | Signed URL replay | A signed download URL must not be indefinitely reusable after the caller's access is revoked (e.g. after account deletion or entitlement revocation) — verify expiry is enforced server-side, not just by URL obscurity. |
| 14 | Automated tests | Every row above needs an automated regression test before merge, per DEVELOPMENT_INSTRUCTIONS.md §9 ("every behavior change MUST include the smallest effective automated regression test"). |

## Do not build ahead of need

Per DEVELOPMENT_INSTRUCTIONS.md §2 and CLAUDE.md's non-negotiable rules, this checklist is
intentionally **not** accompanied by scaffolding, a chosen storage provider, or placeholder
interfaces beyond what already exists in the empty `Files` module. Building any of that now, with
no consuming feature, would be exactly the kind of speculative infrastructure the project
explicitly rejects.
