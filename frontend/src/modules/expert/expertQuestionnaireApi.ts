import { apiRequest } from "../../shared/api/apiClient";

export type WaitingTimeBucketName = 0 | 1 | 2;

export interface QueueItem {
  submissionId: string;
  userId: string;
  clientEmail: string | null;
  submittedAt: string;
  waitingBucket: WaitingTimeBucketName;
  status: string;
}

export interface QAPair {
  questionId: string;
  questionText: string;
  type: string;
  answerValue: string;
  answerLabels: string[] | null;
}

export interface GuidanceVersion {
  id: string;
  version: number;
  body: string;
  publishedAt: string | null;
  followUp: { id: string; question: string; answer: string | null; answeredAt: string | null } | null;
}

export interface SubmissionDetailForExpert {
  submissionId: string;
  userId: string;
  clientEmail: string | null;
  submittedAt: string | null;
  answers: QAPair[];
  guidanceHistory: GuidanceVersion[];
}

export const expertQuestionnaireApi = {
  getQueue: () => apiRequest<QueueItem[]>("/expert/questionnaires/queue"),

  getSubmission: (submissionId: string) =>
    apiRequest<SubmissionDetailForExpert>(`/expert/questionnaires/submissions/${submissionId}`),

  saveGuidanceDraft: (submissionId: string, body: string) =>
    apiRequest<string>(`/expert/questionnaires/submissions/${submissionId}/guidance`, { method: "PUT", body: { body } }),

  publishGuidance: (guidanceResponseId: string) =>
    apiRequest<void>(`/expert/questionnaires/guidance/${guidanceResponseId}/publish`, { method: "POST" }),

  answerFollowUp: (followUpId: string, answer: string) =>
    apiRequest<void>(`/expert/questionnaires/follow-up/${followUpId}/answer`, { method: "POST", body: { answer } }),
};
