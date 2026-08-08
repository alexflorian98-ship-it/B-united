import { create } from "zustand";
import { decodeAccessToken } from "./jwt";
import { tokenStorage } from "./tokenStorage";
import type { TokenPair } from "./authApi";

export interface CurrentUser {
  id: string;
  email: string;
  permissions: string[];
}

export type SessionStatus = "bootstrapping" | "authenticated" | "unauthenticated";

interface AuthState {
  status: SessionStatus;
  accessToken: string | null;
  user: CurrentUser | null;
  setSession: (tokenPair: TokenPair) => void;
  clearSession: () => void;
  markUnauthenticated: () => void;
}

/**
 * The access token lives ONLY here (in memory) — never in `localStorage`/`sessionStorage`
 * (P1.37.a). It's lost on a full page reload by design; `SessionProvider` re-derives it from the
 * persisted refresh token on startup.
 */
export const useAuthStore = create<AuthState>((set) => ({
  status: "bootstrapping",
  accessToken: null,
  user: null,

  setSession: (tokenPair) => {
    const decoded = decodeAccessToken(tokenPair.accessToken);
    tokenStorage.setRefreshToken(tokenPair.refreshToken);
    set({
      status: "authenticated",
      accessToken: tokenPair.accessToken,
      user: decoded ? { id: decoded.userId, email: decoded.email, permissions: decoded.permissions } : null,
    });
  },

  clearSession: () => {
    tokenStorage.clearRefreshToken();
    set({ status: "unauthenticated", accessToken: null, user: null });
  },

  markUnauthenticated: () => set({ status: "unauthenticated", accessToken: null, user: null }),
}));

export function getAccessToken(): string | null {
  return useAuthStore.getState().accessToken;
}
