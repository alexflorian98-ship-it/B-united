import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { z } from "zod";
import { Alert } from "../../../shared/design-system/Alert";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Input } from "../../../shared/design-system/Input";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { applyApiErrorToForm } from "../../../shared/forms/applyApiErrorToForm";
import { ProgramStatus, adminContentApi } from "../content/adminContentApi";
import { adminBillingApi, type PurchaseSortBy, type PurchaseSummary } from "./adminBillingApi";

const createOfferSchema = z.object({
  programId: z.string().min(1),
  amount: z.coerce.number().positive(),
  currency: z.string().length(3),
  activateImmediately: z.boolean(),
});
type CreateOfferFormInput = z.input<typeof createOfferSchema>;
type CreateOfferValues = z.output<typeof createOfferSchema>;

const updatePriceSchema = z.object({
  amount: z.coerce.number().positive(),
  currency: z.string().length(3),
});
type UpdatePriceFormInput = z.input<typeof updatePriceSchema>;
type UpdatePriceValues = z.output<typeof updatePriceSchema>;

function CreateOfferForm() {
  const { t } = useTranslation(["admin", "common"]);
  const client = useQueryClient();
  const programsQuery = useQuery({
    queryKey: ["admin-programs-for-offer", ProgramStatus.Published],
    queryFn: () => adminContentApi.listPrograms(ProgramStatus.Published, 1, 100),
  });
  const { register, handleSubmit, setError, reset, formState } = useForm<CreateOfferFormInput, unknown, CreateOfferValues>({
    resolver: zodResolver(createOfferSchema),
    defaultValues: { currency: "RON", activateImmediately: false },
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const mutation = useMutation({
    mutationFn: (values: CreateOfferValues) => adminBillingApi.createOffer(values),
    onSuccess: () => {
      setSuccess(true);
      reset({ programId: "", amount: 0, currency: "RON", activateImmediately: false });
      client.invalidateQueries({ queryKey: ["admin-offers"] });
    },
    onError: (error: unknown) => setFormError(applyApiErrorToForm(error, setError, t, ["programId", "amount", "currency"])),
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    setSuccess(false);
    mutation.mutate(values);
  });

  return (
    <Card>
      <h3 className="text-base font-semibold text-text-primary">{t("admin:billing.createOffer.title")}</h3>
      <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
        {formError && <Alert tone="danger" title={formError} />}
        {success && <Alert tone="success" title={t("admin:billing.createOffer.success")} />}

        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:billing.createOffer.program")}</span>
          <select {...register("programId")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
            <option value="">{t("admin:billing.createOffer.selectProgram")}</option>
            {programsQuery.data?.items.map((program) => (
              <option key={program.id} value={program.id}>
                {program.title}
              </option>
            ))}
          </select>
          {formState.errors.programId && <p className="text-xs text-danger">{formState.errors.programId.message}</p>}
        </label>

        <div className="flex gap-3">
          <Input label={t("admin:billing.createOffer.amount")} type="number" step="0.01" min="0" {...register("amount")} error={formState.errors.amount?.message} />
          <Input label={t("admin:billing.createOffer.currency")} {...register("currency")} error={formState.errors.currency?.message} />
        </div>

        <label className="flex items-center gap-2 text-sm text-text-primary">
          <input type="checkbox" {...register("activateImmediately")} />
          {t("admin:billing.createOffer.activateImmediately")}
        </label>

        <Button type="submit" disabled={mutation.isPending}>
          {t("admin:billing.createOffer.submit")}
        </Button>
      </form>
    </Card>
  );
}

function UpdatePriceForm({ offerId, onDone }: { offerId: string; onDone: () => void }) {
  const { t } = useTranslation(["admin", "common"]);
  const client = useQueryClient();
  const { register, handleSubmit, setError, formState } = useForm<UpdatePriceFormInput, unknown, UpdatePriceValues>({
    resolver: zodResolver(updatePriceSchema),
    defaultValues: { currency: "RON" },
  });
  const [formError, setFormError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (values: UpdatePriceValues) => adminBillingApi.updateOfferPrice(offerId, values),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ["admin-offers"] });
      onDone();
    },
    onError: (error: unknown) => setFormError(applyApiErrorToForm(error, setError, t, ["amount", "currency"])),
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-wrap items-end gap-3">
      {formError && <Alert tone="danger" title={formError} />}
      <Input label={t("admin:billing.updatePrice.amount")} type="number" step="0.01" min="0" {...register("amount")} error={formState.errors.amount?.message} />
      <Input label={t("admin:billing.updatePrice.currency")} {...register("currency")} error={formState.errors.currency?.message} />
      <Button type="submit" variant="secondary" disabled={mutation.isPending}>
        {t("admin:billing.updatePrice.submit")}
      </Button>
    </form>
  );
}

const PURCHASE_STATUSES: PurchaseSummary["status"][] = ["Pending", "Succeeded", "Failed", "Refunded", "Chargeback"];
const PAGE_SIZE = 25;

export function AdminBillingListPage() {
  const { t } = useTranslation(["admin", "common"]);
  const client = useQueryClient();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<PurchaseSummary["status"] | "">("");
  const [sortBy, setSortBy] = useState<PurchaseSortBy>("CreatedAt");
  const [descending, setDescending] = useState(true);
  const purchases = useQuery({
    queryKey: ["admin-purchases", page, statusFilter, sortBy, descending],
    queryFn: () => adminBillingApi.listPurchases({ page, pageSize: PAGE_SIZE, status: statusFilter, sortBy, descending }),
  });
  const offers = useQuery({ queryKey: ["admin-offers"], queryFn: adminBillingApi.listOffers });
  const programs = useQuery({
    queryKey: ["admin-programs-all"],
    queryFn: () => adminContentApi.listPrograms(null, 1, 200),
  });
  const status = useMutation({ mutationFn: ({ id, active }: { id: string; active: boolean }) => active ? adminBillingApi.deactivateOffer(id) : adminBillingApi.activateOffer(id), onSuccess: () => client.invalidateQueries({ queryKey: ["admin-offers"] }) });
  const [editingOfferId, setEditingOfferId] = useState<string | null>(null);
  if (offers.isLoading || programs.isLoading) return <Skeleton className="h-64 w-full" />;
  const programTitleById = new Map(programs.data?.items.map((program) => [program.id, program.title]));
  const programLabel = (programId: string) => programTitleById.get(programId) ?? programId;
  // Purchase history carries its own immutable snapshot label (Slice A5) — prefer it over the
  // live catalogue lookup, since the program may since have been renamed/unpublished/archived.
  const purchaseLabel = (programId: string, snapshot: string | null) => snapshot ?? programLabel(programId);
  return <div className="flex flex-col gap-8">
    <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:billing.title")}</h1>
    <CreateOfferForm />
    <section><h2 className="text-lg font-semibold">{t("admin:billing.offers")}</h2>
      {offers.data?.items.length === 0 ? <EmptyState title={t("admin:billing.noOffers")} /> : <div className="mt-3 flex flex-col gap-2">{offers.data?.items.map((offer) => <div key={offer.id} className="flex flex-col gap-2 rounded-lg border border-border-default bg-surface p-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <span className="text-sm font-medium text-text-primary">{programLabel(offer.programId)}</span>
          <span>{offer.currentAmount?.toFixed(2) ?? "—"} {offer.currentCurrency}</span>
          <Badge tone={offer.status === "Active" ? "success" : "neutral"}>{offer.status}</Badge>
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => setEditingOfferId(editingOfferId === offer.id ? null : offer.id)}>{t("admin:billing.updatePrice.title")}</Button>
            <Button variant="secondary" disabled={status.isPending} onClick={() => status.mutate({ id: offer.id, active: offer.status === "Active" })}>{offer.status === "Active" ? t("admin:billing.deactivate") : t("admin:billing.activate")}</Button>
          </div>
        </div>
        {editingOfferId === offer.id && <UpdatePriceForm offerId={offer.id} onDone={() => setEditingOfferId(null)} />}
      </div>)}</div>}
    </section>
    <section><h2 className="text-lg font-semibold">{t("admin:billing.purchases")}</h2>
      <div className="mt-3 flex flex-wrap items-end gap-3">
        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-text-secondary">{t("admin:billing.filters.status")}</span>
          <select
            value={statusFilter}
            onChange={(event) => { setStatusFilter(event.target.value as PurchaseSummary["status"] | ""); setPage(1); }}
            className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
          >
            <option value="">{t("admin:billing.filters.allStatuses")}</option>
            {PURCHASE_STATUSES.map((value) => <option key={value} value={value}>{value}</option>)}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-text-secondary">{t("admin:billing.filters.sortBy")}</span>
          <select
            value={sortBy}
            onChange={(event) => { setSortBy(event.target.value as PurchaseSortBy); setPage(1); }}
            className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
          >
            <option value="CreatedAt">{t("admin:billing.filters.sortByCreatedAt")}</option>
            <option value="Amount">{t("admin:billing.filters.sortByAmount")}</option>
          </select>
        </label>
        <Button variant="secondary" onClick={() => setDescending((current) => !current)}>
          {descending ? t("admin:billing.filters.descending") : t("admin:billing.filters.ascending")}
        </Button>
      </div>
      {purchases.isLoading ? <Skeleton className="mt-3 h-32 w-full" /> : purchases.isError ? <Alert tone="danger" title={t("common:errors.generic")} /> : purchases.data!.items.length === 0 ? <EmptyState title={t("admin:billing.noPurchases")} /> : <>
        <div className="mt-3 overflow-x-auto rounded-lg border border-border-default bg-surface"><table className="w-full text-left text-sm"><caption className="sr-only">{t("admin:billing.purchasesTableCaption")}</caption><thead><tr><th scope="col" className="p-3">{t("admin:billing.columns.email")}</th><th scope="col" className="p-3">{t("admin:billing.columns.program")}</th><th scope="col" className="p-3">{t("admin:billing.columns.amount")}</th><th scope="col" className="p-3">{t("admin:billing.columns.status")}</th><th scope="col" className="p-3">{t("admin:billing.columns.actions")}</th></tr></thead><tbody>{purchases.data!.items.map((p) => <tr key={p.purchaseId} className="border-t border-border-default"><td className="p-3">{p.email ?? p.userId}</td><td className="p-3">{purchaseLabel(p.programId, p.programTitleSnapshot)}</td><td className="p-3">{p.amount.toFixed(2)} {p.currency}</td><td className="p-3">{p.status}</td><td className="p-3"><Link to={`/admin/billing/purchases/${p.purchaseId}`}><Button variant="secondary">{t("admin:billing.view")}</Button></Link></td></tr>)}</tbody></table></div>
        <div className="mt-3 flex items-center justify-between gap-3">
          <span className="text-xs text-text-muted">{t("admin:billing.filters.pageInfo", { page, totalPages: Math.max(1, Math.ceil(purchases.data!.totalCount / PAGE_SIZE)) })}</span>
          <div className="flex gap-2">
            <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>{t("admin:billing.filters.previousPage")}</Button>
            <Button variant="secondary" disabled={page * PAGE_SIZE >= purchases.data!.totalCount} onClick={() => setPage((current) => current + 1)}>{t("admin:billing.filters.nextPage")}</Button>
          </div>
        </div>
      </>}
    </section>
  </div>;
}
