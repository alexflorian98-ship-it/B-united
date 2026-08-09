import { apiRequest } from "../../shared/api/apiClient";

export interface Profile {
  userId: string;
  email: string;
  timezone: string;
  preferredLanguage: string;
  emailNotificationsEnabled: boolean;
}

export interface UpdateProfileInput {
  timezone: string;
  preferredLanguage: string;
  emailNotificationsEnabled: boolean;
}

/** Shape is intentionally loose (`Record<string, unknown>`) — this is a passthrough JSON archive
 * assembled server-side from every module's own export section (docs/PROMPT.md §66); the
 * frontend never needs to read individual fields out of it, only serialize the whole thing back
 * to a downloadable file. */
export type UserDataExport = Record<string, unknown>;

export const profileApi = {
  get: () => apiRequest<Profile>("/profile"),
  update: (input: UpdateProfileInput) => apiRequest<Profile>("/profile", { method: "PUT", body: input }),
  exportData: () => apiRequest<UserDataExport>("/profile/export"),
  deleteAccount: (currentPassword: string) =>
    apiRequest<void>("/profile/delete", { method: "POST", body: { currentPassword } }),
};
