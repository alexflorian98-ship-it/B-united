const REFRESH_TOKEN_KEY = "bunited.refreshToken";

/**
 * Persists ONLY the refresh token — rotating, single-use, server-revocable, so its exposure
 * surface is far smaller than a bearer access token (P1.37.a). The access token itself is never
 * written to any persistent storage; it lives in memory only (see `authStore`).
 */
export const tokenStorage = {
  getRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      return null;
    }
  },

  setRefreshToken(token: string): void {
    try {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } catch {
      // Storage unavailable (private browsing, quota) — the session just won't survive a reload.
    }
  },

  clearRefreshToken(): void {
    try {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
    } catch {
      // Nothing to clean up if storage isn't available in the first place.
    }
  },
};
