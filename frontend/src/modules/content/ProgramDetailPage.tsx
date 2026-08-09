import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import i18n from "../../shared/i18n/i18n";
import { progressApi } from "../progress/progressApi";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import { contentApi } from "./contentApi";
import { billingApi } from "../billing/billingApi";

export function ProgramDetailPage() {
  const { t } = useTranslation(["content", "common"]);
  const { slug = "" } = useParams<{ slug: string }>();
  const language = i18n.resolvedLanguage ?? "ro";
  const queryClient = useQueryClient();

  const programQuery = useQuery({
    queryKey: ["published-program", slug, language],
    queryFn: () => contentApi.getProgram(slug, language),
  });

  const sectionIds = useMemo(() => programQuery.data?.sections.map((s) => s.id) ?? [], [programQuery.data]);
  const sectionProgressQuery = useQuery({
    queryKey: ["section-progress", sectionIds],
    queryFn: () => progressApi.getSectionProgress(sectionIds),
    enabled: sectionIds.length > 0 && programQuery.data?.ownershipState === "Owned",
  });

  const checkout = useMutation({
    mutationFn: (programId: string) => billingApi.checkout(programId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["published-program", slug, language] });
      await queryClient.invalidateQueries({ queryKey: ["published-programs"] });
      await queryClient.invalidateQueries({ queryKey: ["my-purchases"] });
      await queryClient.invalidateQueries({ queryKey: ["my-entitlements"] });
    },
  });

  if (programQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  if (programQuery.isError || !programQuery.data) {
    return <Alert tone="danger" title={t("common:errors.notFound")} />;
  }

  const program = programQuery.data;
  const isOwned = program.ownershipState === "Owned";
  const progressBySection = new Map(sectionProgressQuery.data?.map((p) => [p.sectionId, p]) ?? []);

  const firstIncompleteSection =
    program.sections.find((s) => progressBySection.get(s.id)?.status !== "Completed") ?? program.sections[0];
  const firstItem = firstIncompleteSection?.items[0];
  const hasAnyProgress = sectionProgressQuery.data?.some((p) => p.completedItemCount > 0) ?? false;

  const ctaLabel = program.sections.every((s) => progressBySection.get(s.id)?.status === "Completed")
    ? t("content:cta.completed")
    : hasAnyProgress
      ? t("content:cta.continue")
      : t("content:cta.start");

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-lg bg-primary p-5 shadow-md">
        <h1 className="font-serif text-2xl font-medium text-on-primary tablet:text-3xl">{program.title}</h1>
        <p className="mt-2 max-w-2xl text-sm text-on-primary/80">{program.description}</p>
        {isOwned && firstItem && (
          <Link
            to={`/programs/${program.slug}/learn/${firstItem.id}`}
            className="mt-5 inline-flex min-h-11 items-center justify-center rounded-full bg-accent px-5 py-2.5 text-sm font-medium text-on-accent hover:bg-accent-hover"
          >
            {ctaLabel}
          </Link>
        )}
        {!isOwned && program.activeOffer && <Button
          variant="accent"
          className="mt-5 border-2 border-on-primary/25 px-6 font-semibold shadow-md hover:-translate-y-0.5 hover:shadow-lg"
          disabled={checkout.isPending}
          onClick={() => checkout.mutate(program.id)}
        >
          {checkout.isPending ? t("content:paywall.processing") : t("content:paywall.buy", { amount: program.activeOffer.amount.toFixed(2), currency: program.activeOffer.currency })}
        </Button>}
        {!isOwned && !program.activeOffer && <p className="mt-4 text-sm text-on-primary/80">{t("content:paywall.unavailable")}</p>}
        {checkout.isError && <div className="mt-3"><Alert tone="danger" title={t("content:paywall.failed")} /></div>}
      </div>

      <div className="flex flex-col gap-3">
        {program.sections.map((section, index) => {
          const sectionProgress = progressBySection.get(section.id);
          return (
            <Card key={section.id} className="flex items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-background font-serif text-sm font-medium text-text-secondary">
                  {index + 1}
                </span>
                <div>
                  <h2 className="text-sm font-semibold text-text-primary">{section.title}</h2>
                  <p className="text-xs text-text-muted">{t("content:itemCount", { count: section.items.length })}</p>
                </div>
              </div>
              {isOwned && <StatusBadge
                status={sectionProgress?.status === "Completed" ? "success" : sectionProgress?.status === "InProgress" ? "info" : "neutral"}
                label={t(`content:progressStatus.${sectionProgress?.status ?? "NotStarted"}`)}
              />}
            </Card>
          );
        })}
      </div>

      {!firstItem && <Button variant="secondary" disabled>{t("content:noContent")}</Button>}
    </div>
  );
}
