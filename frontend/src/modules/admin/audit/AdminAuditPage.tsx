import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Input } from "../../../shared/design-system/Input";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { adminAuditApi, type AuditLogFilters } from "./adminAuditApi";

const PAGE_SIZE = 25;

function toUtcIsoOrUndefined(dateOnly: string): string | undefined {
  return dateOnly ? new Date(`${dateOnly}T00:00:00.000Z`).toISOString() : undefined;
}

export function AdminAuditPage() {
  const { t } = useTranslation(["admin", "common"]);
  const [action, setAction] = useState("");
  const [entityType, setEntityType] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);

  const filters: AuditLogFilters = {
    action: action || undefined,
    entityType: entityType || undefined,
    fromUtc: toUtcIsoOrUndefined(fromDate),
    toUtc: toUtcIsoOrUndefined(toDate),
  };

  const auditQuery = useQuery({
    queryKey: ["admin-audit", filters, page],
    queryFn: () => adminAuditApi.list(filters, page, PAGE_SIZE),
  });

  const totalPages = auditQuery.data ? Math.max(1, Math.ceil(auditQuery.data.totalCount / PAGE_SIZE)) : 1;

  const resetPage = () => setPage(1);

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:audit.title")}</h1>

      <div className="flex flex-wrap items-end gap-3">
        <Input
          label={t("admin:audit.filters.action")}
          value={action}
          onChange={(event) => {
            setAction(event.target.value);
            resetPage();
          }}
          placeholder={t("admin:audit.filters.actionPlaceholder")}
        />
        <Input
          label={t("admin:audit.filters.entityType")}
          value={entityType}
          onChange={(event) => {
            setEntityType(event.target.value);
            resetPage();
          }}
          placeholder={t("admin:audit.filters.entityTypePlaceholder")}
        />
        <Input
          label={t("admin:audit.filters.from")}
          type="date"
          value={fromDate}
          onChange={(event) => {
            setFromDate(event.target.value);
            resetPage();
          }}
        />
        <Input
          label={t("admin:audit.filters.to")}
          type="date"
          value={toDate}
          onChange={(event) => {
            setToDate(event.target.value);
            resetPage();
          }}
        />
      </div>

      {auditQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {auditQuery.isError && <EmptyState title={t("admin:audit.loadError")} />}

      {auditQuery.isSuccess && auditQuery.data.items.length === 0 && <EmptyState title={t("admin:audit.noEntries")} />}

      {auditQuery.isSuccess && auditQuery.data.items.length > 0 && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border-default bg-surface shadow-sm">
            <table className="w-full text-left text-sm">
              <caption className="sr-only">{t("admin:audit.tableCaption")}</caption>
              <thead className="bg-background text-xs uppercase tracking-wide text-text-muted">
                <tr>
                  <th scope="col" className="px-3 py-2">{t("admin:audit.columns.timestamp")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:audit.columns.action")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:audit.columns.actor")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:audit.columns.entity")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:audit.columns.metadata")}</th>
                </tr>
              </thead>
              <tbody>
                {auditQuery.data.items.map((entry) => (
                  <tr key={entry.id} className="border-t border-border-default align-top">
                    <td className="whitespace-nowrap px-3 py-2 text-text-muted">{new Date(entry.timestampUtc).toLocaleString()}</td>
                    <td className="px-3 py-2"><Badge tone="neutral">{entry.action}</Badge></td>
                    <td className="px-3 py-2">{entry.actorEmail ?? entry.actorUserId ?? t("admin:audit.systemActor")}</td>
                    <td className="px-3 py-2">{entry.entityType ? `${entry.entityType} · ${entry.entityId}` : "—"}</td>
                    <td className="px-3 py-2">
                      {entry.metadata && Object.keys(entry.metadata).length > 0
                        ? Object.entries(entry.metadata).map(([key, value]) => (
                            <div key={key} className="text-xs text-text-muted">
                              <span className="font-medium text-text-secondary">{key}:</span> {value}
                            </div>
                          ))
                        : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between gap-3">
            <span className="text-sm text-text-muted">{t("admin:audit.pageOf", { page, totalPages })}</span>
            <div className="flex gap-2">
              <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
                {t("admin:audit.previous")}
              </Button>
              <Button variant="secondary" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>
                {t("admin:audit.next")}
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
