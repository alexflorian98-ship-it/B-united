import { apiRequest } from "../../../shared/api/apiClient";

export const ProgramStatus = { Draft: 0, Published: 1, Archived: 2 } as const;
export type ProgramStatusValue = (typeof ProgramStatus)[keyof typeof ProgramStatus];

export const ContentItemType = { Video: 0, RichText: 1 } as const;
export type ContentItemTypeValue = (typeof ContentItemType)[keyof typeof ContentItemType];

export interface ProgramSummary {
  id: string;
  slug: string;
  status: ProgramStatusValue;
  defaultLanguage: string;
  domainId: string;
  sortOrder: number;
  title: string;
  sectionCount: number;
  languages: string[];
  updatedAt: string;
}

export interface ProgramListResult {
  items: ProgramSummary[];
  totalCount: number;
}

export interface ProgramTranslation {
  language: string;
  title: string;
  shortDescription: string;
  description: string;
}

export interface SectionTranslation {
  language: string;
  title: string;
  description: string;
}

export interface ContentItemTranslation {
  language: string;
  title: string;
  body: string | null;
}

export interface ContentItemDetail {
  id: string;
  type: ContentItemTypeValue;
  sortOrder: number;
  isRequired: boolean;
  mediaAssetId: string | null;
  translations: ContentItemTranslation[];
}

export interface SectionDetail {
  id: string;
  sortOrder: number;
  status: ProgramStatusValue;
  translations: SectionTranslation[];
  items: ContentItemDetail[];
}

export interface ProgramDetail {
  id: string;
  domainId: string;
  slug: string;
  status: ProgramStatusValue;
  defaultLanguage: string;
  coverAssetId: string | null;
  sortOrder: number;
  translations: ProgramTranslation[];
  sections: SectionDetail[];
}

export const adminContentApi = {
  listPrograms: (status: ProgramStatusValue | null, page: number, pageSize: number) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (status !== null) params.set("status", String(status));
    return apiRequest<ProgramListResult>(`/admin/content/programs?${params.toString()}`);
  },

  getProgram: (programId: string) => apiRequest<ProgramDetail>(`/admin/content/programs/${programId}`),

  createProgram: (input: { domainId: string; slug: string; defaultLanguage: string; title: string; shortDescription: string; description: string }) =>
    apiRequest<string>("/admin/content/programs", { method: "POST", body: input }),

  upsertProgramTranslation: (
    programId: string,
    input: { language: string; title: string; shortDescription: string; description: string },
  ) => apiRequest<void>(`/admin/content/programs/${programId}/translations`, { method: "PUT", body: input }),

  publishProgram: (programId: string) => apiRequest<void>(`/admin/content/programs/${programId}/publish`, { method: "POST" }),
  unpublishProgram: (programId: string) => apiRequest<void>(`/admin/content/programs/${programId}/unpublish`, { method: "POST" }),
  archiveProgram: (programId: string) => apiRequest<void>(`/admin/content/programs/${programId}/archive`, { method: "POST" }),

  reorderSections: (programId: string, orderedSectionIds: string[]) =>
    apiRequest<void>(`/admin/content/programs/${programId}/sections/reorder`, {
      method: "POST",
      body: { orderedSectionIds },
    }),

  addSection: (programId: string, input: { language: string; title: string; description: string }) =>
    apiRequest<string>(`/admin/content/programs/${programId}/sections`, { method: "POST", body: input }),

  upsertSectionTranslation: (sectionId: string, input: { language: string; title: string; description: string }) =>
    apiRequest<void>(`/admin/content/sections/${sectionId}/translations`, { method: "PUT", body: input }),

  deleteSection: (sectionId: string) => apiRequest<void>(`/admin/content/sections/${sectionId}`, { method: "DELETE" }),

  reorderContentItems: (sectionId: string, orderedContentItemIds: string[]) =>
    apiRequest<void>(`/admin/content/sections/${sectionId}/content-items/reorder`, {
      method: "POST",
      body: { orderedContentItemIds },
    }),

  addContentItem: (
    sectionId: string,
    input: { type: ContentItemTypeValue; isRequired: boolean; language: string; title: string; body: string | null; videoReference: string | null },
  ) => apiRequest<string>(`/admin/content/sections/${sectionId}/content-items`, { method: "POST", body: input }),

  upsertContentItemTranslation: (contentItemId: string, input: { language: string; title: string; body: string | null }) =>
    apiRequest<void>(`/admin/content/content-items/${contentItemId}/translations`, { method: "PUT", body: input }),

  deleteContentItem: (contentItemId: string) => apiRequest<void>(`/admin/content/content-items/${contentItemId}`, { method: "DELETE" }),
};
