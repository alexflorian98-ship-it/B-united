import { apiRequest } from "../../../shared/api/apiClient";

export interface AuditLogEntry {
  id: string;
  action: string;
  timestampUtc: string;
  actorUserId: string | null;
  actorEmail: string | null;
  entityType: string | null;
  entityId: string | null;
  correlationId: string | null;
  metadata: Record<string, string> | null;
}

export interface AuditLogListResult {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuditLogFilters {
  action?: string;
  actorUserId?: string;
  entityType?: string;
  fromUtc?: string;
  toUtc?: string;
}

function buildQuery(filters: AuditLogFilters, page: number, pageSize: number): string {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (filters.action) params.set("action", filters.action);
  if (filters.actorUserId) params.set("actorUserId", filters.actorUserId);
  if (filters.entityType) params.set("entityType", filters.entityType);
  if (filters.fromUtc) params.set("fromUtc", filters.fromUtc);
  if (filters.toUtc) params.set("toUtc", filters.toUtc);
  return params.toString();
}

export const adminAuditApi = {
  list: (filters: AuditLogFilters, page = 1, pageSize = 25) =>
    apiRequest<AuditLogListResult>(`/admin/audit?${buildQuery(filters, page, pageSize)}`),
};
