import { apiRequest } from "../api/apiClient";

export interface TokenPair {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface RegisterResult {
  userId: string;
  email: string;
}

/** Thin, typed wrappers around the Identity module's `/api/v1/auth/*` endpoints. */
export const authApi = {
  register: (email: string, password: string) =>
    apiRequest<RegisterResult>("/auth/register", { method: "POST", body: { email, password }, skipAuth: true }),

  verifyEmail: (token: string) => apiRequest<void>("/auth/verify-email", { method: "POST", body: { token }, skipAuth: true }),

  resendVerification: (email: string) =>
    apiRequest<void>("/auth/resend-verification", { method: "POST", body: { email }, skipAuth: true }),

  login: (email: string, password: string) =>
    apiRequest<TokenPair>("/auth/login", { method: "POST", body: { email, password }, skipAuth: true }),

  refresh: (refreshToken: string) =>
    apiRequest<TokenPair>("/auth/refresh", { method: "POST", body: { refreshToken }, skipAuth: true }),

  revoke: (refreshToken: string) =>
    apiRequest<void>("/auth/revoke", { method: "POST", body: { refreshToken }, skipAuth: true }),

  requestPasswordReset: (email: string) =>
    apiRequest<void>("/auth/password-reset/request", { method: "POST", body: { email }, skipAuth: true }),

  confirmPasswordReset: (token: string, newPassword: string) =>
    apiRequest<void>("/auth/password-reset/confirm", {
      method: "POST",
      body: { token, newPassword },
      skipAuth: true,
    }),
};
