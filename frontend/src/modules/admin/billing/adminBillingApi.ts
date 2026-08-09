import { apiRequest } from "../../../shared/api/apiClient";
import type { Purchase } from "../../billing/billingApi";

export interface Payment { id: string; amount: number; currency: string; status: string; occurredAtUtc: string }
export interface Invoice { id: string; amount: number; currency: string; status: string; issuedAtUtc: string }
export interface PurchaseSummary extends Omit<Purchase, "id"> { purchaseId: string; userId: string; email: string | null }
export interface PurchaseListResult { items: PurchaseSummary[]; totalCount: number }
export interface WebhookEventSummary { id: string; eventType: string; providerTimestampUtc: string; processedAtUtc: string | null; payloadJson: string | null }
export interface PurchaseDetail extends PurchaseSummary { entitlementStatus: string | null; payments: Payment[]; invoices: Invoice[]; webhookTimeline: WebhookEventSummary[] }
export interface ProgramOfferSummary { id: string; programId: string; status: string; currentAmount: number | null; currentCurrency: string | null; createdAt: string; updatedAt: string }
export interface ProgramOfferListResult { items: ProgramOfferSummary[]; totalCount: number }

export interface CreateProgramOfferRequest { programId: string; amount: number; currency: string; activateImmediately: boolean }
export interface UpdateProgramOfferPriceRequest { amount: number; currency: string }

export const adminBillingApi = {
  listPurchases: (page = 1, pageSize = 25) => apiRequest<PurchaseListResult>(`/admin/billing/purchases?page=${page}&pageSize=${pageSize}`),
  getPurchase: (purchaseId: string) => apiRequest<PurchaseDetail>(`/admin/billing/purchases/${purchaseId}`),
  listOffers: () => apiRequest<ProgramOfferListResult>("/admin/billing/offers"),
  createOffer: (request: CreateProgramOfferRequest) => apiRequest<{ programOfferId: string }>("/admin/billing/offers", { method: "POST", body: request }),
  updateOfferPrice: (offerId: string, request: UpdateProgramOfferPriceRequest) => apiRequest<string>(`/admin/billing/offers/${offerId}/price`, { method: "PUT", body: request }),
  activateOffer: (offerId: string) => apiRequest<void>(`/admin/billing/offers/${offerId}/activate`, { method: "POST" }),
  deactivateOffer: (offerId: string) => apiRequest<void>(`/admin/billing/offers/${offerId}/deactivate`, { method: "POST" }),
};
