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

export const profileApi = {
  get: () => apiRequest<Profile>("/profile"),
  update: (input: UpdateProfileInput) => apiRequest<Profile>("/profile", { method: "PUT", body: input }),
};
