import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
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

export function ProgramDetailPage() {
  const { t } = useTranslation(["content", "common"]);
  const { slug = "" } = useParams<{ slug: string }>();
  const language = i18n.resolvedLanguage ?? "ro";

  const programQuery = useQuery({
    queryKey: ["published-program", slug, language],
    queryFn: () => contentApi.getProgram(slug, language),
  });

  const sectionIds = useMemo(() => programQuery.data?.sections.map((s) => s.id) ?? [], [programQuery.data]);
  const sectionProgressQuery = useQuery({
    queryKey: ["section-progress", sectionIds],
    queryFn: () => progressApi.getSectionProgress(sectionIds),
    enabled: sectionIds.length > 0,
  });

  if (programQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3 p-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  if (programQuery.isError || !programQuery.data) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.notFound")} />
      </div>
    );
  }

  const program = programQuery.data;
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
    <div className="flex flex-col gap-4 p-4">
      <Card>
        <h1 className="text-lg font-semibold text-text-primary">{program.title}</h1>
        <p className="mt-2 text-sm text-text-secondary">{program.description}</p>
        {firstItem && (
          <Link
            to={`/programs/${program.slug}/learn/${firstItem.id}`}
            className="mt-4 inline-flex min-h-11 items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-hover"
          >
            {ctaLabel}
          </Link>
        )}
      </Card>

      <div className="flex flex-col gap-2">
        {program.sections.map((section) => {
          const sectionProgress = progressBySection.get(section.id);
          return (
            <Card key={section.id} className="flex items-center justify-between gap-3">
              <div>
                <h2 className="text-sm font-semibold text-text-primary">{section.title}</h2>
                <p className="text-xs text-text-muted">{t("content:itemCount", { count: section.items.length })}</p>
              </div>
              <StatusBadge
                status={sectionProgress?.status === "Completed" ? "success" : sectionProgress?.status === "InProgress" ? "info" : "neutral"}
                label={t(`content:progressStatus.${sectionProgress?.status ?? "NotStarted"}`)}
              />
            </Card>
          );
        })}
      </div>

      {!firstItem && <Button variant="secondary" disabled>{t("content:noContent")}</Button>}
    </div>
  );
}
