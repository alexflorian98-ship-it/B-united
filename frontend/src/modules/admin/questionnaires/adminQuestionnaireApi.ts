import { apiRequest } from "../../../shared/api/apiClient";

export type QuestionnaireStatusName = "Draft" | "Published" | "Archived";
export type QuestionTypeName = "Text" | "LongText" | "SingleChoice" | "MultiChoice" | "Scale";

export interface QuestionnaireSummary {
  id: string;
  status: QuestionnaireStatusName;
  defaultLanguage: string;
  title: string;
  questionCount: number;
  languages: string[];
  updatedAt: string;
}

export interface QuestionnaireListResult {
  items: QuestionnaireSummary[];
  totalCount: number;
}

export interface QuestionnaireTranslation {
  language: string;
  title: string;
  description: string;
}

export interface QuestionOptionTranslation {
  language: string;
  label: string;
}

export interface QuestionOptionDetail {
  id: string;
  value: string;
  sortOrder: number;
  translations: QuestionOptionTranslation[];
}

export interface QuestionTranslation {
  language: string;
  text: string;
  helpText: string | null;
}

export interface QuestionDetail {
  id: string;
  type: QuestionTypeName;
  sortOrder: number;
  isRequired: boolean;
  translations: QuestionTranslation[];
  options: QuestionOptionDetail[];
}

export interface QuestionnaireDetail {
  id: string;
  status: QuestionnaireStatusName;
  defaultLanguage: string;
  translations: QuestionnaireTranslation[];
  questions: QuestionDetail[];
}

export const adminQuestionnaireApi = {
  list: (status?: QuestionnaireStatusName) => {
    const params = status ? `?status=${status}` : "";
    return apiRequest<QuestionnaireListResult>(`/admin/questionnaires${params}`);
  },

  get: (questionnaireId: string) => apiRequest<QuestionnaireDetail>(`/admin/questionnaires/${questionnaireId}`),

  create: (defaultLanguage: string, title: string, description: string) =>
    apiRequest<string>("/admin/questionnaires", { method: "POST", body: { defaultLanguage, title, description } }),

  upsertTranslation: (questionnaireId: string, language: string, title: string, description: string) =>
    apiRequest<void>(`/admin/questionnaires/${questionnaireId}/translations`, { method: "PUT", body: { language, title, description } }),

  publish: (questionnaireId: string) => apiRequest<void>(`/admin/questionnaires/${questionnaireId}/publish`, { method: "POST" }),
  unpublish: (questionnaireId: string) => apiRequest<void>(`/admin/questionnaires/${questionnaireId}/unpublish`, { method: "POST" }),
  archive: (questionnaireId: string) => apiRequest<void>(`/admin/questionnaires/${questionnaireId}/archive`, { method: "POST" }),

  reorderQuestions: (questionnaireId: string, orderedQuestionIds: string[]) =>
    apiRequest<void>(`/admin/questionnaires/${questionnaireId}/questions/reorder`, { method: "POST", body: { orderedQuestionIds } }),

  addQuestion: (questionnaireId: string, type: QuestionTypeName, isRequired: boolean, text: string, helpText: string | null) =>
    apiRequest<string>(`/admin/questionnaires/${questionnaireId}/questions`, { method: "POST", body: { type, isRequired, text, helpText } }),

  upsertQuestionTranslation: (questionId: string, language: string, text: string, helpText: string | null) =>
    apiRequest<void>(`/admin/questionnaires/questions/${questionId}/translations`, { method: "PUT", body: { language, text, helpText } }),

  deleteQuestion: (questionId: string) =>
    apiRequest<void>(`/admin/questionnaires/questions/${questionId}`, { method: "DELETE" }),

  addQuestionOption: (questionId: string, value: string, label: string) =>
    apiRequest<string>(`/admin/questionnaires/questions/${questionId}/options`, { method: "POST", body: { value, label } }),

  upsertQuestionOptionTranslation: (optionId: string, language: string, label: string) =>
    apiRequest<void>(`/admin/questionnaires/options/${optionId}/translations`, { method: "PUT", body: { language, label } }),

  deleteQuestionOption: (optionId: string) =>
    apiRequest<void>(`/admin/questionnaires/options/${optionId}`, { method: "DELETE" }),
};
