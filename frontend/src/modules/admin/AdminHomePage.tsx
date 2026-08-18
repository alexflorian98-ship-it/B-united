import { useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { useCurrentUser } from "../../shared/auth/useCurrentUser";
import { Alert } from "../../shared/design-system/Alert";
import { Badge } from "../../shared/design-system/Badge";
import { Card } from "../../shared/design-system/Card";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { adminDashboardApi } from "./dashboard/adminDashboardApi";

function formatDateTime(isoUtc: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(isoUtc));
}

function formatMoney(amount: number, currency: string, locale: string): string {
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(amount);
}

function KpiCard({ label, value }: { label: string; value: string }) {
  return (
    <Card className="flex flex-col gap-1">
      <span className="text-sm text-text-muted">{label}</span>
      <span className="text-2xl font-semibold text-text-primary">{value}</span>
    </Card>
  );
}

function WidgetCard({ title, viewAllLabel, viewAllTo, isEmpty, emptyLabel, children }: {
  title: string;
  viewAllLabel: string;
  viewAllTo: string;
  isEmpty: boolean;
  emptyLabel: string;
  children: ReactNode;
}) {
  return (
    <Card className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-base font-semibold text-text-primary">{title}</h3>
        <Link to={viewAllTo} className="text-sm font-medium text-primary hover:underline">
          {viewAllLabel}
        </Link>
      </div>
      {isEmpty ? <EmptyState title={emptyLabel} /> : <div className="flex flex-col gap-2">{children}</div>}
    </Card>
  );
}

/** docs/PROMPT.md §442 — the expert/admin dashboard: KPI cards plus the "requires attention"
 * widgets (pending questionnaires, upcoming events, recent purchases/refunds, reported chat
 * messages, recently published content). Data comes from the read-only cross-module
 * <c>GET /api/v1/admin/dashboard</c> projection (see ADR-007,
 * BUnited.Modules.Admin.Application.UseCases.GetDashboardHandler). */
export function AdminHomePage() {
  const { t, i18n } = useTranslation(["dashboard", "common", "billing"]);
  const user = useCurrentUser();
  const locale = i18n.resolvedLanguage ?? "ro";

  const dashboardQuery = useQuery({
    queryKey: ["admin-dashboard"],
    queryFn: adminDashboardApi.getDashboard,
  });

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-lg bg-primary p-5 shadow-md">
        <h1 className="text-2xl font-semibold text-on-primary tablet:text-3xl">
          {t("dashboard:greeting", { email: user?.email ?? "" })}
        </h1>
      </div>

      <h2 className="text-lg font-semibold text-text-primary">{t("dashboard:adminOverview.title")}</h2>

      {dashboardQuery.isLoading && (
        <div className="grid grid-cols-1 gap-4 tablet:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full" />
          ))}
        </div>
      )}

      {dashboardQuery.isError && <Alert tone="danger" title={t("dashboard:adminOverview.loadError")} />}

      {dashboardQuery.data && (
        <>
          <div className="grid grid-cols-1 gap-4 tablet:grid-cols-3">
            <KpiCard label={t("dashboard:adminOverview.kpis.customersWithPurchases")} value={String(dashboardQuery.data.kpis.customersWithPurchases)} />
            <KpiCard label={t("dashboard:adminOverview.kpis.completedPurchases")} value={String(dashboardQuery.data.kpis.completedPurchases)} />
            <KpiCard label={t("dashboard:adminOverview.kpis.pendingQuestionnaires")} value={String(dashboardQuery.data.kpis.pendingQuestionnaires)} />
            <KpiCard label={t("dashboard:adminOverview.kpis.upcomingEvents")} value={String(dashboardQuery.data.kpis.upcomingEventsCount)} />
            <KpiCard
              label={t("dashboard:adminOverview.kpis.revenue")}
              value={
                dashboardQuery.data.kpis.revenueByCurrency.length === 0
                  ? "—"
                  : dashboardQuery.data.kpis.revenueByCurrency.map((r) => formatMoney(r.amount, r.currency, locale)).join(" · ")
              }
            />
          </div>

          <div className="grid grid-cols-1 gap-4 tablet:grid-cols-2">
            <WidgetCard
              title={t("dashboard:adminOverview.questionnaires.title")}
              viewAllLabel={t("dashboard:adminOverview.questionnaires.viewQueue")}
              viewAllTo="/admin/questionnaires/queue"
              isEmpty={dashboardQuery.data.questionnaires.pendingCount === 0}
              emptyLabel={t("dashboard:adminOverview.questionnaires.empty")}
            >
              {dashboardQuery.data.questionnaires.oldest && (
                <div className="flex items-center justify-between gap-2 rounded-md border border-warning bg-surface p-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-text-primary">{dashboardQuery.data.questionnaires.oldest.userEmail ?? dashboardQuery.data.questionnaires.oldest.userId}</p>
                    <p className="truncate text-xs text-text-muted">
                      {t("dashboard:adminOverview.questionnaires.oldest", { date: formatDateTime(dashboardQuery.data.questionnaires.oldest.submittedAtUtc, locale) })}
                    </p>
                  </div>
                  <Badge tone="warning" className="shrink-0">{dashboardQuery.data.questionnaires.pendingCount}</Badge>
                </div>
              )}
            </WidgetCard>

            <WidgetCard
              title={t("dashboard:adminOverview.events.title")}
              viewAllLabel={t("dashboard:adminOverview.events.viewAll")}
              viewAllTo="/admin/events"
              isEmpty={dashboardQuery.data.upcomingEvents.length === 0}
              emptyLabel={t("dashboard:adminOverview.events.empty")}
            >
              {dashboardQuery.data.upcomingEvents.map((event) => (
                <div key={event.eventId} className="flex items-center justify-between gap-2 rounded-md border border-border-default p-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-text-primary">{event.title ?? event.eventId}</p>
                    <p className="truncate text-xs text-text-muted">{formatDateTime(event.startsAtUtc, locale)}</p>
                  </div>
                  {event.capacity !== null && <Badge tone="neutral" className="shrink-0">{t("dashboard:adminOverview.events.capacity", { capacity: event.capacity })}</Badge>}
                </div>
              ))}
            </WidgetCard>

            <WidgetCard
              title={t("dashboard:adminOverview.purchases.title")}
              viewAllLabel={t("dashboard:adminOverview.purchases.viewAll")}
              viewAllTo="/admin/billing"
              isEmpty={dashboardQuery.data.recentPurchases.length === 0}
              emptyLabel={t("dashboard:adminOverview.purchases.empty")}
            >
              {dashboardQuery.data.recentPurchases.map((purchase) => (
                <Link
                  key={purchase.purchaseId}
                  to={`/admin/billing/purchases/${purchase.purchaseId}`}
                  className="flex items-center justify-between gap-2 rounded-md border border-border-default p-3 hover:bg-background"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-text-primary">{purchase.userEmail ?? purchase.userId}</p>
                    <p className="truncate text-xs text-text-muted">{purchase.programTitleSnapshot ?? purchase.programSlug ?? purchase.programId}</p>
                  </div>
                  <div className="flex shrink-0 items-center gap-2">
                    <span className="whitespace-nowrap text-sm">{formatMoney(purchase.amount, purchase.currency, locale)}</span>
                    <Badge tone={purchase.status === "Refunded" || purchase.status === "Chargeback" ? "danger" : "success"}>
                      {t(`billing:purchase.status.${purchase.status}`)}
                    </Badge>
                  </div>
                </Link>
              ))}
            </WidgetCard>

            <WidgetCard
              title={t("dashboard:adminOverview.reports.title")}
              viewAllLabel={t("dashboard:adminOverview.reports.viewAll")}
              viewAllTo="/admin/community"
              isEmpty={dashboardQuery.data.openChatReports.length === 0}
              emptyLabel={t("dashboard:adminOverview.reports.empty")}
            >
              {dashboardQuery.data.openChatReports.map((report) => (
                <div key={report.reportId} className="flex items-center justify-between gap-2 rounded-md border border-danger p-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-text-primary">{report.reporterEmail ?? report.reporterUserId}</p>
                    <p className="truncate text-xs text-text-muted">{report.reason}</p>
                  </div>
                  <span className="shrink-0 whitespace-nowrap text-xs text-text-muted">{formatDateTime(report.createdAtUtc, locale)}</span>
                </div>
              ))}
            </WidgetCard>

            <WidgetCard
              title={t("dashboard:adminOverview.content.title")}
              viewAllLabel={t("dashboard:adminOverview.content.viewAll")}
              viewAllTo="/admin/programs"
              isEmpty={dashboardQuery.data.recentlyPublishedPrograms.length === 0}
              emptyLabel={t("dashboard:adminOverview.content.empty")}
            >
              {dashboardQuery.data.recentlyPublishedPrograms.map((program) => (
                <div key={program.programId} className="flex items-center justify-between gap-2 rounded-md border border-border-default p-3">
                  <span className="min-w-0 truncate text-sm font-medium text-text-primary">{program.slug}</span>
                  <span className="shrink-0 whitespace-nowrap text-xs text-text-muted">{formatDateTime(program.updatedAtUtc, locale)}</span>
                </div>
              ))}
            </WidgetCard>
          </div>
        </>
      )}
    </div>
  );
}
