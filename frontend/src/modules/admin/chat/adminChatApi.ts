import { apiRequest } from "../../../shared/api/apiClient";

export interface ReportSummary {
  reportId: string;
  messageId: string;
  messageBody: string | null;
  messageAuthorUserId: string;
  messageAuthorEmail: string | null;
  reporterUserId: string;
  reporterEmail: string | null;
  reason: string;
  status: "Open" | "Dismissed" | "Resolved";
  createdAt: string;
}

export interface MutedUserSummary {
  muteId: string;
  userId: string;
  email: string | null;
  reason: string | null;
  expiresAtUtc: string;
  moderatorEmail: string | null;
}

export interface ModeratorAction {
  kind: string;
  actorEmail: string | null;
  targetDescription: string;
  occurredAtUtc: string;
}

// ReportResolutionAction binds via the JSON request body, where the backend's raw C# enum
// requires a numeric value (no global string-enum converter is configured).
export const ReportResolutionAction = { Dismiss: 0, DeleteMessage: 1, MuteUser: 2 } as const;
export type ReportResolutionActionValue = (typeof ReportResolutionAction)[keyof typeof ReportResolutionAction];

export const adminChatApi = {
  listReports: (status?: "Open" | "Dismissed" | "Resolved") =>
    apiRequest<ReportSummary[]>(`/admin/chat/reports${status ? `?status=${status}` : ""}`),

  resolveReport: (reportId: string, action: ReportResolutionActionValue, muteDurationMinutes = 60, muteReason?: string) =>
    apiRequest<void>(`/admin/chat/reports/${reportId}/resolve`, { method: "POST", body: { action, muteDurationMinutes, muteReason } }),

  listMutedUsers: () => apiRequest<MutedUserSummary[]>("/admin/chat/muted-users"),

  listRecentActions: () => apiRequest<ModeratorAction[]>("/admin/chat/recent-actions"),
};
