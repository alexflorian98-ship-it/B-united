import { apiRequest } from "../../../shared/api/apiClient";

export interface RoleSummary {
  id: string;
  name: string;
}

export interface ClientListItem {
  id: string;
  email: string;
  isActive: boolean;
  isEmailVerified: boolean;
  createdAt: string;
  roles: RoleSummary[];
}

export interface ClientListResult {
  items: ClientListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ClientDetail {
  id: string;
  email: string;
  isActive: boolean;
  isEmailVerified: boolean;
  emailVerifiedAtUtc: string | null;
  createdAt: string;
  roles: RoleSummary[];
}

export interface ClientPurchaseSummary {
  purchaseId: string;
  programId: string;
  programSlug: string | null;
  programTitleSnapshot: string | null;
  amount: number;
  currency: string;
  status: string;
  createdAt: string;
  completedAtUtc: string | null;
}

export interface ClientEntitlementSummary {
  programId: string;
  programSlug: string | null;
  status: string;
  grantedAtUtc: string;
  revokedAtUtc: string | null;
}

export interface ClientCommerceSummary {
  purchases: ClientPurchaseSummary[];
  entitlements: ClientEntitlementSummary[];
}

function buildListQuery(search: string | null, roleId: string | null, page: number, pageSize: number): string {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (search) params.set("search", search);
  if (roleId) params.set("roleId", roleId);
  return params.toString();
}

export const adminUsersApi = {
  listClients: (search: string | null, roleId: string | null, page = 1, pageSize = 20) =>
    apiRequest<ClientListResult>(`/admin/users?${buildListQuery(search, roleId, page, pageSize)}`),
  getClient: (userId: string) => apiRequest<ClientDetail>(`/admin/users/${userId}`),
  listRoles: () => apiRequest<RoleSummary[]>("/admin/roles"),
  assignRole: (userId: string, roleId: string) =>
    apiRequest<void>(`/admin/users/${userId}/roles`, { method: "POST", body: { roleId } }),
  removeRole: (userId: string, roleId: string) =>
    apiRequest<void>(`/admin/users/${userId}/roles/${roleId}`, { method: "DELETE" }),
  getCommerceSummary: (userId: string) => apiRequest<ClientCommerceSummary>(`/admin/clients/${userId}/commerce-summary`),
};
