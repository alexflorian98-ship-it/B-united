import { apiRequest } from "../../../shared/api/apiClient";

export interface RevenueByCurrency {
  currency: string;
  amount: number;
}

export interface DashboardKpis {
  customersWithPurchases: number;
  completedPurchases: number;
  pendingQuestionnaires: number;
  upcomingEventsCount: number;
  revenueByCurrency: RevenueByCurrency[];
}

export interface OldestPendingSubmission {
  submissionId: string;
  userId: string;
  userEmail: string | null;
  submittedAtUtc: string;
}

export interface QuestionnaireQueueSummary {
  pendingCount: number;
  oldest: OldestPendingSubmission | null;
}

export interface UpcomingEvent {
  eventId: string;
  title: string | null;
  startsAtUtc: string;
  displayTimezone: string;
  capacity: number | null;
}

export interface RecentPurchase {
  purchaseId: string;
  userId: string;
  userEmail: string | null;
  programId: string;
  programSlug: string | null;
  programTitleSnapshot: string | null;
  amount: number;
  currency: string;
  status: string;
  completedAtUtc: string | null;
}

export interface OpenChatReport {
  reportId: string;
  messageId: string;
  reporterUserId: string;
  reporterEmail: string | null;
  reason: string;
  createdAtUtc: string;
}

export interface RecentlyPublishedProgram {
  programId: string;
  slug: string;
  updatedAtUtc: string;
}

export interface AdminDashboard {
  kpis: DashboardKpis;
  questionnaires: QuestionnaireQueueSummary;
  upcomingEvents: UpcomingEvent[];
  recentPurchases: RecentPurchase[];
  openChatReports: OpenChatReport[];
  recentlyPublishedPrograms: RecentlyPublishedProgram[];
}

export const adminDashboardApi = {
  getDashboard: () => apiRequest<AdminDashboard>("/admin/dashboard"),
};
