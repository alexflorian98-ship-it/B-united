import { apiRequest } from "../../shared/api/apiClient";

export type QuestionTypeName = "Text" | "LongText" | "SingleChoice" | "MultiChoice" | "Scale";
export type SubmissionStatusName = "Draft" | "Submitted" | "Answered";

export interface ClientQuestionOption {
  value: string;
  label: string;
}

export interface ClientQuestion {
  id: string;
  type: QuestionTypeName;
  isRequired: boolean;
  sortOrder: number;
  text: string;
  helpText: string | null;
  options: ClientQuestionOption[];
}

export interface ClientQuestionnaire {
  id: string;
  title: string;
  description: string;
  questions: ClientQuestion[];
}

export interface SubmissionAnswer {
  questionId: string;
  value: string;
}

export interface MySubmission {
  submissionId: string;
  questionnaireId: string;
  status: SubmissionStatusName;
  startedAt: string | null;
  submittedAt: string | null;
  answers: SubmissionAnswer[];
}

export interface GuidanceFollowUp {
  id: string;
  question: string;
  answer: string | null;
  answeredAt: string | null;
}

export interface Guidance {
  id: string;
  submissionId: string;
  version: number;
  body: string;
  publishedAt: string;
  followUp: GuidanceFollowUp | null;
}

export interface PublishedQuestionnaireSummary {
  id: string;
  title: string;
  description: string;
}

export const questionnaireApi = {
  recordConsent: () => apiRequest<void>("/questionnaires/consent", { method: "POST" }),

  listPublished: (language: string) =>
    apiRequest<PublishedQuestionnaireSummary[]>(`/questionnaires?language=${encodeURIComponent(language)}`),

  getQuestionnaire: (questionnaireId: string, language: string) =>
    apiRequest<ClientQuestionnaire>(`/questionnaires/${questionnaireId}?language=${encodeURIComponent(language)}`),

  start: (questionnaireId: string) =>
    apiRequest<string>(`/questionnaires/${questionnaireId}/start`, { method: "POST" }),

  listMySubmissions: () => apiRequest<MySubmission[]>("/questionnaires/submissions"),

  getSubmission: (submissionId: string) =>
    apiRequest<MySubmission>(`/questionnaires/submissions/${submissionId}`),

  saveDraftAnswers: (submissionId: string, answers: SubmissionAnswer[]) =>
    apiRequest<void>(`/questionnaires/submissions/${submissionId}/answers`, { method: "PUT", body: { answers } }),

  submit: (submissionId: string) =>
    apiRequest<void>(`/questionnaires/submissions/${submissionId}/submit`, { method: "POST" }),

  getGuidance: (submissionId: string) =>
    apiRequest<Guidance | null>(`/questionnaires/submissions/${submissionId}/guidance`),

  submitFollowUp: (guidanceResponseId: string, question: string) =>
    apiRequest<void>(`/questionnaires/guidance/${guidanceResponseId}/follow-up`, { method: "POST", body: { question } }),
};
