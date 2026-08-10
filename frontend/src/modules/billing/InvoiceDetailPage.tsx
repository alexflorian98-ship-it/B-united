import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import i18n from "../../shared/i18n/i18n";
import { billingApi } from "./billingApi";

export function InvoiceDetailPage() {
  const { t } = useTranslation(["billing", "common"]);
  const locale = i18n.resolvedLanguage ?? "ro";
  const { invoiceId = "" } = useParams<{ invoiceId: string }>();
  const query = useQuery({
    queryKey: ["my-invoice", invoiceId],
    queryFn: () => billingApi.getMyInvoice(invoiceId),
  });

  if (query.isLoading) return <Skeleton className="h-64 w-full" />;
  if (query.isError || !query.data) return <Alert tone="danger" title={t("common:errors.notFound")} />;

  const invoice = query.data;
  const formatDateTime = (value: string) => new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
  const formatMoney = (amount: number, currency: string) => new Intl.NumberFormat(locale, { style: "currency", currency }).format(amount);

  return <div className="flex flex-col gap-6">
    <div>
      <Link to="/billing" className="text-sm text-text-secondary hover:underline">{t("billing:invoiceDetail.back")}</Link>
      <h1 className="mt-2 text-2xl font-semibold text-text-primary tablet:text-3xl">{t("billing:invoiceDetail.title")}</h1>
    </div>
    <Card className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="text-sm font-medium text-text-primary">{invoice.programTitleSnapshot ?? t("billing:myPurchases.programUnavailable")}</span>
        <StatusBadge status={invoice.status === "Paid" ? "success" : "neutral"} label={invoice.status} />
      </div>
      <dl className="grid grid-cols-2 gap-3 text-sm">
        <dt className="text-text-muted">{t("billing:invoiceDetail.amount")}</dt>
        <dd className="text-right font-semibold text-text-primary">{formatMoney(invoice.amount, invoice.currency)}</dd>
        <dt className="text-text-muted">{t("billing:invoiceDetail.issuedAt")}</dt>
        <dd className="text-right text-text-primary">{formatDateTime(invoice.issuedAtUtc)}</dd>
        <dt className="text-text-muted">{t("billing:invoiceDetail.invoiceId")}</dt>
        <dd className="text-right text-text-primary">{invoice.id}</dd>
      </dl>
    </Card>
    <Link to="/billing"><Button variant="secondary">{t("billing:invoiceDetail.back")}</Button></Link>
  </div>;
}
