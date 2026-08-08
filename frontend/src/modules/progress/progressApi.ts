import { apiRequest } from "../../shared/api/apiClient";

export type ProgressStatusName = "NotStarted" | "InProgress" | "Completed";

export interface ContentProgressEntry {
  contentItemId: string;
  status: ProgressStatusName;
  lastVideoPositionSeconds: number | null;
  watchPercentage: number | null;
}

export interface SectionProgressEntry {
  sectionId: string;
  status: ProgressStatusName;
  completedItemCount: number;
  totalItemCount: number;
}

function idsQuery(ids: string[]): string {
  const params = new URLSearchParams();
  for (const id of ids) params.append("ids", id);
  return params.toString();
}

export const progressApi = {
  recordVideoPosition: (input: {
    contentItemId: string;
    sectionId: string;
    sectionContentItemIds: string[];
    positionSeconds: number;
    watchPercentage: number;
  }) => apiRequest<void>("/progress/video-position", { method: "POST", body: input }),

  markCompleted: (input: { contentItemId: string; sectionId: string; sectionContentItemIds: string[] }) =>
    apiRequest<void>("/progress/complete", { method: "POST", body: input }),

  getContentProgress: (contentItemIds: string[]) =>
    contentItemIds.length === 0
      ? Promise.resolve<ContentProgressEntry[]>([])
      : apiRequest<ContentProgressEntry[]>(`/progress/content-items?${idsQuery(contentItemIds)}`),

  getSectionProgress: (sectionIds: string[]) =>
    sectionIds.length === 0
      ? Promise.resolve<SectionProgressEntry[]>([])
      : apiRequest<SectionProgressEntry[]>(`/progress/sections?${idsQuery(sectionIds)}`),
};
