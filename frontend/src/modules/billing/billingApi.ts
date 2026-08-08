import { apiRequest } from "../../shared/api/apiClient";

// CheckoutOutcome binds via the JSON request body, where the backend's raw C# enum requires a
// numeric value (no global string-enum converter is configured) — unlike DemoAction below,
// which binds via a route segment and accepts the string name directly.
export const CheckoutOutcome = { Success: 0, Decline: 1, ProviderError: 2, Timeout: 3 } as const;
export type CheckoutOutcomeValue = (typeof CheckoutOutcome)[keyof typeof CheckoutOutcome];

export type DemoActionName = "Renew" | "FailPayment" | "Cancel" | "Expire";

export interface PlanPrice {
  id: string;
  amount: number;
  currency: string;
  interval: "Monthly" | "Yearly";
}

export interface Plan {
  id: string;
  name: string;
  description: string;
  prices: PlanPrice[];
}

export interface SubscriptionPeriod {
  periodStart: string;
  periodEnd: string;
  paidPeriodEnd: string;
}

export interface Payment {
  id: string;
  amount: number;
  currency: string;
  status: "Pending" | "Succeeded" | "Failed";
  occurredAtUtc: string;
}

export interface Invoice {
  id: string;
  amount: number;
  currency: string;
  status: "Open" | "Paid" | "Void";
  issuedAtUtc: string;
}

export type SubscriptionStatusName = "Trialing" | "Active" | "PastDue" | "Canceled" | "Expired";

export interface MyBillingStatus {
  subscriptionId: string | null;
  planName: string | null;
  status: SubscriptionStatusName | null;
  currentPeriod: SubscriptionPeriod | null;
  hasActiveAccess: boolean;
  accessUntilUtc: string | null;
  payments: Payment[];
  invoices: Invoice[];
}

export interface CheckoutResult {
  subscriptionId: string;
  activated: boolean;
  declineReason: string | null;
}

export const billingApi = {
  listPlans: () => apiRequest<Plan[]>("/billing/plans"),

  getStatus: () => apiRequest<MyBillingStatus>("/billing/status"),

  checkout: (planPriceId: string, outcome: CheckoutOutcomeValue = CheckoutOutcome.Success) =>
    apiRequest<CheckoutResult>("/billing/checkout", { method: "POST", body: { planPriceId, outcome } }),

  triggerDemoAction: (action: DemoActionName) =>
    apiRequest<void>(`/billing/demo/${action}`, { method: "POST" }),
};
