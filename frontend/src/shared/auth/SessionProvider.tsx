import { useEffect, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { configureApiClient } from "../api/apiClient";
import { authApi } from "./authApi";
import { tokenStorage } from "./tokenStorage";
import { getAccessToken, useAuthStore } from "./authStore";

let refreshInFlight: Promise<string | null> | null = null;

/**
 * Single-flight by itself — not just relying on `apiClient`'s own dedup, which only guards its
 * internal 401-retry path. `attemptRefresh` is ALSO called directly by `SessionProvider`'s
 * bootstrap effect, and React 19 `StrictMode` double-invokes effects in development; without
 * this, two concurrent bootstrap calls would each read the same not-yet-rotated refresh token,
 * and the second one would be treated as token *reuse* — which revokes the whole family,
 * killing the very session the first (legitimate) call just established. Found via live
 * testing, not by inspection (see docs/HANDOVER.md).
 */
async function attemptRefresh(): Promise<string | null> {
  refreshInFlight ??= performRefresh().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

async function performRefresh(): Promise<string | null> {
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) {
    useAuthStore.getState().markUnauthenticated();
    return null;
  }

  try {
    const tokenPair = await authApi.refresh(refreshToken);
    useAuthStore.getState().setSession(tokenPair);
    return tokenPair.accessToken;
  } catch {
    // Expired/revoked/reused refresh token — nothing to recover from, only to clear.
    useAuthStore.getState().clearSession();
    return null;
  }
}

// Wired once, at module scope, so it's in place before the very first query/component render —
// avoids a "was the client configured yet?" race with React's render/effect timing.
configureApiClient({
  getAccessToken,
  onUnauthorized: attemptRefresh,
});

export interface SessionProviderProps {
  children: ReactNode;
}

/**
 * Bootstraps the session (silent refresh from the persisted refresh token) once at startup and
 * only renders `children` once that has settled either way — otherwise a route guard would
 * flash protected content, or flash the login screen for an already-logged-in user, before the
 * refresh call comes back (P1.37.a, P1.40.c).
 */
export function SessionProvider({ children }: SessionProviderProps) {
  const { t } = useTranslation();
  const status = useAuthStore((state) => state.status);

  useEffect(() => {
    void attemptRefresh();
  }, []);

  if (status === "bootstrapping") {
    return (
      <div className="flex min-h-screen items-center justify-center" role="status" aria-live="polite">
        <span className="text-sm text-text-muted">{t("status.loading")}</span>
      </div>
    );
  }

  return children;
}
