export interface DecodedAccessToken {
  userId: string;
  email: string;
  permissions: string[];
  expiresAtUtc: number;
}

/**
 * Decodes (never verifies) a JWT payload, for UI purposes only — showing the user's email,
 * hiding nav items they lack permission for, etc. The server is the sole authority on whether
 * a token is actually valid; this is never used to make an access decision.
 */
export function decodeAccessToken(token: string): DecodedAccessToken | null {
  try {
    const [, payloadSegment] = token.split(".");
    if (!payloadSegment) {
      return null;
    }

    const normalized = payloadSegment.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
    const json = decodeURIComponent(
      atob(padded)
        .split("")
        .map((char) => "%" + char.charCodeAt(0).toString(16).padStart(2, "0"))
        .join(""),
    );
    const payload = JSON.parse(json) as Record<string, unknown>;

    const permissionClaim = payload.permission;
    const permissions = Array.isArray(permissionClaim)
      ? permissionClaim.filter((value): value is string => typeof value === "string")
      : typeof permissionClaim === "string"
        ? [permissionClaim]
        : [];

    return {
      userId: String(payload.sub ?? ""),
      email: String(payload.email ?? ""),
      permissions,
      expiresAtUtc: Number(payload.exp ?? 0),
    };
  } catch {
    return null;
  }
}
