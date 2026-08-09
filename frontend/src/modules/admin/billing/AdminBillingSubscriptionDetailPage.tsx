import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Alert } from "../../../shared/design-system/Alert";
import { Card } from "../../../shared/design-system/Card";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { StatusBadge } from "../../../shared/design-system/StatusBadge";
import { adminBillingApi } from "./adminBillingApi";

export function AdminBillingSubscriptionDetailPage() {
  const { t } = useTranslation(["admin", "common"]);
  const { purchaseId = "" } = useParams<{ purchaseId: string }>();
  const query = useQuery({ queryKey: ["admin-purchase", purchaseId], queryFn: () => adminBillingApi.getPurchase(purchaseId) });
  if (query.isLoading) return <Skeleton className="h-64 w-full" />;
  if (!query.data) return <Alert tone="danger" title={t("common:errors.notFound")} />;
  const detail = query.data;
  return <div className="flex flex-col gap-6"><div><h1 className="text-2xl font-semibold">{detail.email ?? detail.userId}</h1><p className="text-sm text-text-muted">{detail.programTitleSnapshot ?? detail.programId}</p></div>
    <Card className="flex flex-wrap items-center justify-between gap-3"><span>{detail.amount.toFixed(2)} {detail.currency}</span><StatusBadge status={detail.status === "Succeeded" ? "success" : detail.status === "Failed" ? "danger" : "neutral"} label={detail.status} /><span>{detail.entitlementStatus ?? "—"}</span></Card>
    <section><h2 className="font-semibold">{t("admin:billing.payments")}</h2>{detail.payments.map((p) => <Card key={p.id} className="mt-2 flex justify-between"><span>{new Date(p.occurredAtUtc).toLocaleString()}</span><span>{p.amount.toFixed(2)} {p.currency}</span><span>{p.status}</span></Card>)}</section>
    <section><h2 className="font-semibold">{t("admin:billing.invoices")}</h2>{detail.invoices.map((i) => <Card key={i.id} className="mt-2 flex justify-between"><span>{new Date(i.issuedAtUtc).toLocaleString()}</span><span>{i.amount.toFixed(2)} {i.currency}</span><span>{i.status}</span></Card>)}</section>
    <section><h2 className="font-semibold">{t("admin:billing.webhookTimeline")}</h2>{detail.webhookTimeline.map((event) => <Card key={event.id} className="mt-2"><div className="flex justify-between"><span>{event.eventType}</span><span>{new Date(event.providerTimestampUtc).toLocaleString()}</span></div>{event.payloadJson && <pre className="mt-2 overflow-x-auto text-xs">{event.payloadJson}</pre>}</Card>)}</section>
  </div>;
}
