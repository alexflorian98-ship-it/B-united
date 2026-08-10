import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import i18n from "../../shared/i18n/i18n";
import { contentApi } from "../content/contentApi";
import { billingApi, type DemoActionName, type PurchaseStatusName } from "./billingApi";

const tone = (status: PurchaseStatusName) => status === "Succeeded" ? "success" : status === "Pending" ? "warning" : status === "Failed" ? "danger" : "neutral";

export function BillingPage() {
  const { t } = useTranslation(["billing", "common"]);
  const queryClient = useQueryClient();
  const locale = i18n.resolvedLanguage ?? "ro";
  const purchases = useQuery({ queryKey: ["my-purchases"], queryFn: billingApi.listMyPurchases });
  const entitlements = useQuery({ queryKey: ["my-entitlements"], queryFn: billingApi.listMyEntitlements });
  const invoices = useQuery({ queryKey: ["my-invoices"], queryFn: billingApi.listMyInvoices });
  const programs = useQuery({ queryKey: ["published-programs", null, locale], queryFn: () => contentApi.listPrograms(null, locale) });
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["my-purchases"] });
    void queryClient.invalidateQueries({ queryKey: ["my-entitlements"] });
    void queryClient.invalidateQueries({ queryKey: ["published-programs"] });
    void queryClient.invalidateQueries({ queryKey: ["published-program"] });
  };
  const demo = useMutation({ mutationFn: ({ programId, action }: { programId: string; action: DemoActionName }) => billingApi.triggerDemoAction(programId, action), onSuccess: refresh });

  if (purchases.isLoading || entitlements.isLoading || invoices.isLoading) return <Skeleton className="h-64 w-full" />;
  if (purchases.isError || entitlements.isError || invoices.isError) return <Alert tone="danger" title={t("common:errors.generic")} />;

  const programTitles = new Map(programs.data?.map((program) => [program.id, program.title]) ?? []);
  // Historical purchases/invoices carry their own snapshot label (Slice A5) — prefer it over the
  // live published catalogue, since a purchase's program may since have been renamed, unpublished,
  // or archived and would otherwise show as unavailable despite still being owned.
  const snapshotTitles = new Map(
    purchases.data!.filter((purchase) => purchase.programTitleSnapshot).map((purchase) => [purchase.programId, purchase.programTitleSnapshot as string]),
  );
  const formatDateTime = (value: string) => new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
  const formatMoney = (amount: number, currency: string) => new Intl.NumberFormat(locale, { style: "currency", currency }).format(amount);
  const programLabel = (programId: string, snapshot?: string | null) =>
    snapshot ?? snapshotTitles.get(programId) ?? programTitles.get(programId) ?? t("billing:myPurchases.programUnavailable");

  return <div className="flex flex-col gap-6">
    <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("billing:title")}</h1>
    <section>
      <h2 className="text-lg font-semibold text-text-primary">{t("billing:myPurchases.title")}</h2>
      {purchases.data!.length === 0 ? <EmptyState title={t("billing:myPurchases.empty")} /> : <div className="mt-3 flex flex-col gap-3">
        {purchases.data!.map((purchase) => <Card key={purchase.id}>
          <div className="grid gap-3 tablet:grid-cols-[minmax(0,1fr)_auto_auto] tablet:items-center">
            <div className="min-w-0"><p className="truncate text-sm font-semibold text-text-primary">{programLabel(purchase.programId, purchase.programTitleSnapshot)}</p><p className="text-xs text-text-muted">{formatDateTime(purchase.createdAt)}</p></div>
            <span className="font-semibold text-text-primary">{formatMoney(purchase.amount, purchase.currency)}</span>
            <StatusBadge status={tone(purchase.status)} label={t(`billing:purchase.status.${purchase.status}`)} />
          </div>
          <details className="mt-4 border-t border-border-default pt-3">
            <summary className="cursor-pointer text-sm font-medium text-text-secondary">{t("billing:demo.title")}</summary>
            <div className="mt-3 flex flex-wrap gap-2">
              {(["Succeed", "Fail", "Refund", "ChargeBack"] as DemoActionName[]).map((action) => <Button key={action} variant="secondary" disabled={demo.isPending} onClick={() => demo.mutate({ programId: purchase.programId, action })}>{t(`billing:demo.${action}`)}</Button>)}
            </div>
          </details>
        </Card>)}
      </div>}
    </section>
    <section>
      <h2 className="text-lg font-semibold text-text-primary">{t("billing:ownedPrograms.title")}</h2>
      <div className="mt-3 flex flex-col gap-2">{entitlements.data!.map((item) => <Card key={item.programId} className="flex items-center justify-between gap-3"><span className="min-w-0 truncate font-medium">{programLabel(item.programId)}</span><StatusBadge status={item.status === "Active" ? "success" : "neutral"} label={t(`billing:entitlement.status.${item.status}`)} /></Card>)}</div>
    </section>
    <section><h2 className="text-lg font-semibold text-text-primary">{t("billing:invoices.title")}</h2><div className="mt-3 flex flex-col gap-2">{invoices.data!.map((invoice) => <Link key={invoice.id} to={`/billing/invoices/${invoice.id}`}><Card className="grid gap-2 tablet:grid-cols-[minmax(0,1fr)_auto_auto_auto] tablet:items-center transition-colors hover:border-border-strong"><span className="min-w-0 truncate">{programLabel(invoice.programId, invoice.programTitleSnapshot)}</span><span>{formatDateTime(invoice.issuedAtUtc)}</span><span className="font-medium">{formatMoney(invoice.amount, invoice.currency)}</span><span>{invoice.status}</span></Card></Link>)}</div></section>
  </div>;
}
