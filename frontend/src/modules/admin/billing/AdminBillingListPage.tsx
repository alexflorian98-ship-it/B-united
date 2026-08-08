import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { adminBillingApi } from "./adminBillingApi";

/** §54: subscriber table (Subscriber, Email, Status, Current Period, Access Until, Payment
 * State, Created). */
export function AdminBillingListPage() {
  const { t } = useTranslation(["admin", "common"]);

  const listQuery = useQuery({
    queryKey: ["admin-subscribers"],
    queryFn: () => adminBillingApi.listSubscribers(),
  });

  return (
    <div className="flex flex-col gap-4 p-4">
      <h1 className="text-lg font-semibold text-text-primary">{t("admin:billing.title")}</h1>

      {listQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {listQuery.isSuccess && listQuery.data.items.length === 0 && <EmptyState title={t("admin:billing.noSubscribers")} />}

      {listQuery.isSuccess && listQuery.data.items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border-default">
          <table className="w-full text-left text-sm">
            <thead className="bg-background text-xs uppercase text-text-muted">
              <tr>
                <th className="px-3 py-2">{t("admin:billing.columns.email")}</th>
                <th className="px-3 py-2">{t("admin:billing.columns.status")}</th>
                <th className="px-3 py-2">{t("admin:billing.columns.currentPeriodEnd")}</th>
                <th className="px-3 py-2">{t("admin:billing.columns.accessUntil")}</th>
                <th className="px-3 py-2">{t("admin:billing.columns.paymentState")}</th>
                <th className="px-3 py-2">{t("admin:billing.columns.created")}</th>
                <th className="px-3 py-2">{t("admin:billing.columns.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {listQuery.data.items.map((subscriber) => (
                <tr key={subscriber.subscriptionId} className="border-t border-border-default">
                  <td className="px-3 py-2 font-medium text-text-primary">{subscriber.email ?? subscriber.userId}</td>
                  <td className="px-3 py-2">
                    <Badge tone={subscriber.status === "Active" ? "success" : subscriber.status === "Expired" ? "danger" : "warning"}>
                      {t(`admin:billing.status.${subscriber.status.toLowerCase()}`)}
                    </Badge>
                  </td>
                  <td className="px-3 py-2 text-text-muted">
                    {subscriber.currentPeriodEnd ? new Date(subscriber.currentPeriodEnd).toLocaleDateString() : "—"}
                  </td>
                  <td className="px-3 py-2 text-text-muted">
                    {subscriber.accessUntilUtc ? new Date(subscriber.accessUntilUtc).toLocaleDateString() : "—"}
                  </td>
                  <td className="px-3 py-2 text-text-muted">{subscriber.paymentState}</td>
                  <td className="px-3 py-2 text-text-muted">{new Date(subscriber.createdAt).toLocaleDateString()}</td>
                  <td className="px-3 py-2">
                    <Link to={`/admin/billing/${subscriber.subscriptionId}`}>
                      <Button variant="secondary">{t("admin:billing.view")}</Button>
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
