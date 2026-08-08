import { apiRequest } from "../../shared/api/apiClient";

export interface ContentDomain {
  id: string;
  slug: string;
  sortOrder: number;
}

export interface ClientProgramSummary {
  id: string;
  slug: string;
  domainId: string;
  title: string;
  shortDescription: string;
  coverAssetId: string | null;
  sortOrder: number;
}

export type ContentItemTypeName = "Video" | "RichText";

export interface ClientContentItem {
  id: string;
  type: ContentItemTypeName;
  sortOrder: number;
  isRequired: boolean;
  title: string;
  body: string | null;
  mediaAssetId: string | null;
}

export interface ClientSection {
  id: string;
  sortOrder: number;
  title: string;
  description: string;
  items: ClientContentItem[];
}

export interface ClientProgramDetail {
  id: string;
  slug: string;
  domainId: string;
  title: string;
  shortDescription: string;
  description: string;
  coverAssetId: string | null;
  sections: ClientSection[];
}

export interface VideoPlaybackResult {
  playbackUrl: string;
  thumbnailUrl: string | null;
}

export const contentApi = {
  listDomains: () => apiRequest<ContentDomain[]>("/content/domains"),

  listPrograms: (domainId: string | null, language: string) => {
    const params = new URLSearchParams({ language });
    if (domainId) params.set("domainId", domainId);
    return apiRequest<ClientProgramSummary[]>(`/content/programs?${params.toString()}`);
  },

  getProgram: (slug: string, language: string) =>
    apiRequest<ClientProgramDetail>(`/content/programs/${encodeURIComponent(slug)}?language=${encodeURIComponent(language)}`),

  getVideoPlayback: (contentItemId: string) =>
    apiRequest<VideoPlaybackResult>(`/content/content-items/${contentItemId}/playback`),
};
