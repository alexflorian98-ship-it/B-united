import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Alert } from "../../../shared/design-system/Alert";
import { Card } from "../../../shared/design-system/Card";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { StatusBadge } from "../../../shared/design-system/StatusBadge";
import { adminBillingApi } from "./adminBillingApi";

/** §54 subscription detail: plan, status, period, payments, invoices, entitlement, webhook
 * timeline. Raw webhook payloads (P3.22) are shown only when the backend actually returns them
 * — gated server-side on `billing.view_raw_webhook_payloads`, never hidden by the frontend alone. */
export function AdminBillingSubscriptionDetailPage() {
  const { t } = useTranslation(["admin", "common"]);
  const { subscriptionId = "" } = useParams<{ subscriptionId: string }>();

  const detailQuery = useQuery({
    queryKey: ["admin-subscription", subscriptionId],
    queryFn: () => adminBillingApi.getSubscription(subscriptionId),
  });

  if (detailQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3 p-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (!detailQuery.data) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.notFound")} />
      </div>
    );
  }

  const detail = detailQuery.data;

  return (
    <div className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-text-primary">{detail.email ?? detail.userId}</h1>
          <p className="text-sm text-text-secondary">{detail.planName}</p>
        </div>
        <StatusBadge status={detail.status === "Active" ? "success" : detail.status === "Expired" ? "danger" : "warning"} label={detail.status} />
      </div>

      {detail.currentPeriod && (
        <Card>
          <p className="text-sm text-text-secondary">
            {t("admin:billing.columns.currentPeriodEnd")}: {new Date(detail.currentPeriod.periodEnd).toLocaleDateString()}
          </p>
          {detail.accessUntilUtc && (
            <p className="mt-1 text-sm text-text-secondary">
              {t("admin:billing.columns.accessUntil")}: {new Date(detail.accessUntilUtc).toLocaleDateString()}
            </p>
          )}
        </Card>
      )}

      <div>
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:billing.payments")}</h2>
        <div className="mt-2 flex flex-col gap-2">
          {detail.payments.map((payment) => (
            <Card key={payment.id} className="flex items-center justify-between">
              <span className="text-sm text-text-secondary">{new Date(payment.occurredAtUtc).toLocaleString()}</span>
              <span className="text-sm font-medium text-text-primary">
                {payment.amount.toFixed(2)} {payment.currency}
              </span>
              <StatusBadge status={payment.status === "Succeeded" ? "success" : payment.status === "Failed" ? "danger" : "neutral"} label={payment.status} />
            </Card>
          ))}
        </div>
      </div>

      <div>
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:billing.webhookTimeline")}</h2>
        <div className="mt-2 flex flex-col gap-2">
          {detail.webhookTimeline.map((event) => (
            <Card key={event.id}>
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-text-primary">{event.eventType}</span>
                <span className="text-xs text-text-muted">{new Date(event.providerTimestampUtc).toLocaleString()}</span>
              </div>
              <p className="mt-1 text-xs text-text-muted">
                {event.processedAtUtc ? t("admin:billing.processed") : t("admin:billing.notProcessed")}
              </p>
              {event.payloadJson && (
                <pre className="mt-2 overflow-x-auto rounded-md bg-background p-2 text-xs text-text-secondary">{event.payloadJson}</pre>
              )}
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}
