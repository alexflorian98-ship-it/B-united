import { apiRequest } from "../../../shared/api/apiClient";
import type { Invoice, Payment, SubscriptionPeriod } from "../../billing/billingApi";

export interface SubscriberSummary {
  userId: string;
  email: string | null;
  subscriptionId: string;
  status: string;
  currentPeriodEnd: string | null;
  accessUntilUtc: string | null;
  paymentState: string;
  createdAt: string;
}

export interface SubscriberListResult {
  items: SubscriberSummary[];
  totalCount: number;
}

export interface WebhookEventSummary {
  id: string;
  eventType: string;
  providerTimestampUtc: string;
  processedAtUtc: string | null;
  payloadJson: string | null;
}

export interface SubscriptionDetail {
  subscriptionId: string;
  userId: string;
  email: string | null;
  planName: string;
  status: string;
  currentPeriod: SubscriptionPeriod | null;
  accessUntilUtc: string | null;
  payments: Payment[];
  invoices: Invoice[];
  webhookTimeline: WebhookEventSummary[];
}

export const adminBillingApi = {
  listSubscribers: (page = 1, pageSize = 25) =>
    apiRequest<SubscriberListResult>(`/admin/billing/subscribers?page=${page}&pageSize=${pageSize}`),

  getSubscription: (subscriptionId: string) =>
    apiRequest<SubscriptionDetail>(`/admin/billing/subscribers/${subscriptionId}`),
};
