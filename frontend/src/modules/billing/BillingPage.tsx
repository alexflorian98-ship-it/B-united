import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import { billingApi, type DemoActionName, type SubscriptionStatusName } from "./billingApi";

const STATUS_TONE: Record<SubscriptionStatusName, "success" | "warning" | "danger" | "info" | "neutral"> = {
  Trialing: "info",
  Active: "success",
  PastDue: "warning",
  Canceled: "neutral",
  Expired: "danger",
};

/** docs/PROMPT.md §17/P3.17: client billing screen. Every demo action (P3.18) is clearly
 * labeled as a simulation — see ADR-010, no real money ever moves in V1. */
export function BillingPage() {
  const { t } = useTranslation(["billing", "common"]);
  const queryClient = useQueryClient();

  const statusQuery = useQuery({ queryKey: ["billing-status"], queryFn: () => billingApi.getStatus() });
  const plansQuery = useQuery({ queryKey: ["billing-plans"], queryFn: () => billingApi.listPlans() });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["billing-status"] });

  const checkoutMutation = useMutation({
    mutationFn: (planPriceId: string) => billingApi.checkout(planPriceId),
    onSuccess: invalidate,
  });

  const demoActionMutation = useMutation({
    mutationFn: (action: DemoActionName) => billingApi.triggerDemoAction(action),
    onSuccess: invalidate,
  });

  if (statusQuery.isLoading || plansQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3 p-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (statusQuery.isError || plansQuery.isError) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.generic")} />
      </div>
    );
  }

  const status = statusQuery.data!;
  const plans = plansQuery.data!;
  const currentStatus = status.status;

  const canFailPayment = currentStatus === "Trialing" || currentStatus === "Active";
  const canCancel = currentStatus === "Trialing" || currentStatus === "Active" || currentStatus === "PastDue";
  const canRenew = currentStatus !== null && currentStatus !== "Canceled";
  const canExpire = currentStatus !== null && currentStatus !== "Expired";

  return (
    <div className="flex flex-col gap-4 p-4">
      <h1 className="text-lg font-semibold text-text-primary">{t("billing:title")}</h1>

      {!status.subscriptionId ? (
        <div className="flex flex-col gap-3">
          <p className="text-sm text-text-secondary">{t("billing:noSubscription")}</p>
          {plans.map((plan) => (
            <Card key={plan.id}>
              <h2 className="text-sm font-semibold text-text-primary">{plan.name}</h2>
              <p className="mt-1 text-sm text-text-secondary">{plan.description}</p>
              {plan.prices.map((price) => (
                <div key={price.id} className="mt-3 flex items-center justify-between">
                  <span className="text-sm font-medium text-text-primary">
                    {price.amount.toFixed(2)} {price.currency} / {t(`billing:interval.${price.interval}`)}
                  </span>
                  <Button onClick={() => checkoutMutation.mutate(price.id)} disabled={checkoutMutation.isPending}>
                    {t("billing:subscribe")}
                  </Button>
                </div>
              ))}
            </Card>
          ))}
        </div>
      ) : (
        <>
          <Card>
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-sm font-semibold text-text-primary">{status.planName}</h2>
                {status.currentPeriod && (
                  <p className="mt-1 text-xs text-text-muted">
                    {t("billing:currentPeriod")}: {new Date(status.currentPeriod.periodStart).toLocaleDateString()} –{" "}
                    {new Date(status.currentPeriod.periodEnd).toLocaleDateString()}
                  </p>
                )}
                {status.accessUntilUtc && (
                  <p className="mt-1 text-xs text-text-muted">
                    {t("billing:accessUntil")}: {new Date(status.accessUntilUtc).toLocaleDateString()}
                  </p>
                )}
              </div>
              <StatusBadge status={STATUS_TONE[currentStatus!]} label={t(`billing:status.${currentStatus}`)} />
            </div>
          </Card>

          <Card>
            <h2 className="text-sm font-semibold text-text-primary">{t("billing:demo.title")}</h2>
            <Alert tone="warning" title={t("billing:demo.notice")} />
            <div className="mt-3 flex flex-wrap gap-2">
              <Button variant="secondary" disabled={!canFailPayment || demoActionMutation.isPending} onClick={() => demoActionMutation.mutate("FailPayment")}>
                {t("billing:demo.failPayment")}
              </Button>
              <Button variant="secondary" disabled={!canCancel || demoActionMutation.isPending} onClick={() => demoActionMutation.mutate("Cancel")}>
                {t("billing:demo.cancel")}
              </Button>
              <Button variant="secondary" disabled={!canRenew || demoActionMutation.isPending} onClick={() => demoActionMutation.mutate("Renew")}>
                {t("billing:demo.renew")}
              </Button>
              <Button variant="danger" disabled={!canExpire || demoActionMutation.isPending} onClick={() => demoActionMutation.mutate("Expire")}>
                {t("billing:demo.expire")}
              </Button>
            </div>
          </Card>

          <div>
            <h2 className="text-sm font-semibold text-text-primary">{t("billing:payments")}</h2>
            <div className="mt-2 flex flex-col gap-2">
              {status.payments.length === 0 && <p className="text-sm text-text-muted">{t("billing:noPayments")}</p>}
              {status.payments.map((payment) => (
                <Card key={payment.id} className="flex items-center justify-between">
                  <span className="text-sm text-text-secondary">{new Date(payment.occurredAtUtc).toLocaleString()}</span>
                  <span className="text-sm font-medium text-text-primary">
                    {payment.amount.toFixed(2)} {payment.currency}
                  </span>
                  <StatusBadge
                    status={payment.status === "Succeeded" ? "success" : payment.status === "Failed" ? "danger" : "neutral"}
                    label={t(`billing:paymentStatus.${payment.status}`)}
                  />
                </Card>
              ))}
            </div>
          </div>

          <div>
            <h2 className="text-sm font-semibold text-text-primary">{t("billing:invoices")}</h2>
            <div className="mt-2 flex flex-col gap-2">
              {status.invoices.length === 0 && <p className="text-sm text-text-muted">{t("billing:noInvoices")}</p>}
              {status.invoices.map((invoice) => (
                <Card key={invoice.id} className="flex items-center justify-between">
                  <span className="text-sm text-text-secondary">{new Date(invoice.issuedAtUtc).toLocaleDateString()}</span>
                  <span className="text-sm font-medium text-text-primary">
                    {invoice.amount.toFixed(2)} {invoice.currency}
                  </span>
                  <StatusBadge status={invoice.status === "Paid" ? "success" : "neutral"} label={t(`billing:invoiceStatus.${invoice.status}`)} />
                </Card>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
