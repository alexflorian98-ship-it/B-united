/** A single leading "/" (not "//", which browsers treat as protocol-relative to another host),
 * no whitespace, and no "://" anywhere — i.e. an internal app route and nothing else. */
const SAFE_INTERNAL_PATH = /^\/(?!\/)\S*$/;

/**
 * Only ever returns an internal app path — never trusts `candidate` (which can come from
 * router state seeded by whatever URL a user followed) as a redirect target without checking it
 * first (P1.38.c: "redirect only to an allowlisted internal route").
 */
export function sanitizeRedirectTarget(candidate: unknown, fallback = "/"): string {
  if (typeof candidate !== "string") {
    return fallback;
  }

  if (!SAFE_INTERNAL_PATH.test(candidate) || candidate.includes("://")) {
    return fallback;
  }

  return candidate;
}
