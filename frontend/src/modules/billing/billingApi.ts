import { apiRequest } from "../../shared/api/apiClient";

export const CheckoutOutcome = { Success: 0, Decline: 1, ProviderError: 2, Timeout: 3 } as const;
export type CheckoutOutcomeValue = (typeof CheckoutOutcome)[keyof typeof CheckoutOutcome];
export type DemoActionName = "Succeed" | "Fail" | "Refund" | "ChargeBack";
export type PurchaseStatusName = "Pending" | "Succeeded" | "Failed" | "Refunded" | "Chargeback";

export interface Purchase {
  id: string;
  programId: string;
  /** The program's title at the moment of purchase (docs/IMPLEMENTATION_PLAN.md Slice A5) —
   * immutable afterwards, so historical billing history stays readable even if the program is
   * later renamed, unpublished, or archived. `null` only for purchases made before this field
   * existed and never backfilled, or if no title was resolvable at checkout time. */
  programTitleSnapshot: string | null;
  amount: number;
  currency: string;
  status: PurchaseStatusName;
  createdAt: string;
  completedAtUtc: string | null;
}

export interface ProgramEntitlement {
  programId: string;
  status: "Active" | "Revoked";
  grantedAtUtc: string;
  revokedAtUtc: string | null;
}

export interface CheckoutResult { purchaseId: string; programId: string; status: PurchaseStatusName }
export interface MyInvoice { id: string; purchaseId: string; programId: string; programTitleSnapshot: string | null; amount: number; currency: string; status: string; issuedAtUtc: string }

export const billingApi = {
  listMyPurchases: () => apiRequest<Purchase[]>("/billing/my-purchases"),
  listMyEntitlements: () => apiRequest<ProgramEntitlement[]>("/billing/my-entitlements"),
  listMyInvoices: () => apiRequest<MyInvoice[]>("/billing/my-invoices"),
  getMyInvoice: (invoiceId: string) => apiRequest<MyInvoice>(`/billing/my-invoices/${invoiceId}`),
  checkout: (programId: string, outcome: CheckoutOutcomeValue = CheckoutOutcome.Success) =>
    apiRequest<CheckoutResult>(`/billing/programs/${programId}/checkout`, { method: "POST", body: { outcome } }),
  triggerDemoAction: (programId: string, action: DemoActionName) =>
    apiRequest<void>(`/billing/programs/${programId}/demo/${action}`, { method: "POST" }),
};
