import { apiRequest } from "../../../shared/api/apiClient";

export const ProgramStatus = { Draft: 0, Published: 1, Archived: 2 } as const;
export type ProgramStatusValue = (typeof ProgramStatus)[keyof typeof ProgramStatus];

export const ContentItemType = { Video: 0, RichText: 1, Quiz: 2 } as const;
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

  // Quiz authoring. Quiz items are created empty via `addContentItem` (type: Quiz); questions and
  // options are added afterward via these calls, mirroring how content-item translations are
  // already a separate upsert step. Note: there is currently no admin read endpoint that returns
  // a quiz's existing questions/options (see `ProgramDetailDto`/`GetProgramDetailHandler` — a
  // real backend gap reported alongside this change), so the admin UI can only track what it adds
  // within the current editing session.
  addQuizQuestion: (contentItemId: string, input: { language: string; text: string }) =>
    apiRequest<string>(`/admin/content/content-items/${contentItemId}/quiz/questions`, { method: "POST", body: input }),

  upsertQuizQuestionTranslation: (quizQuestionId: string, input: { language: string; text: string }) =>
    apiRequest<void>(`/admin/content/quiz-questions/${quizQuestionId}/translations`, { method: "PUT", body: input }),

  deleteQuizQuestion: (quizQuestionId: string) => apiRequest<void>(`/admin/content/quiz-questions/${quizQuestionId}`, { method: "DELETE" }),

  reorderQuizQuestions: (contentItemId: string, orderedQuizQuestionIds: string[]) =>
    apiRequest<void>(`/admin/content/content-items/${contentItemId}/quiz/questions/reorder`, {
      method: "POST",
      body: { orderedQuizQuestionIds },
    }),

  // At most one option per question may have `isCorrect: true` — the backend rejects a second one
  // with `QUIZ_OPTION_ALREADY_HAS_CORRECT_ANSWER`. There is no "toggle correct" endpoint, so
  // changing which option is correct requires deleting the current correct option and adding a
  // new one as correct (see `AdminProgramEditorPage.tsx`'s quiz builder UI for how this is
  // surfaced to the admin).
  addQuizOption: (quizQuestionId: string, input: { language: string; label: string; isCorrect: boolean }) =>
    apiRequest<string>(`/admin/content/quiz-questions/${quizQuestionId}/options`, { method: "POST", body: input }),

  upsertQuizOptionTranslation: (quizOptionId: string, input: { language: string; label: string }) =>
    apiRequest<void>(`/admin/content/quiz-options/${quizOptionId}/translations`, { method: "PUT", body: input }),

  deleteQuizOption: (quizOptionId: string) => apiRequest<void>(`/admin/content/quiz-options/${quizOptionId}`, { method: "DELETE" }),

  reorderQuizOptions: (quizQuestionId: string, orderedQuizOptionIds: string[]) =>
    apiRequest<void>(`/admin/content/quiz-questions/${quizQuestionId}/options/reorder`, {
      method: "POST",
      body: { orderedQuizOptionIds },
    }),
};
