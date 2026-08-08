import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import i18n from "../../shared/i18n/i18n";
import { Alert } from "../../shared/design-system/Alert";
import { Card } from "../../shared/design-system/Card";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import { CrisisDisclaimer } from "../../shared/crisis/CrisisDisclaimer";
import { questionnaireApi, type MySubmission } from "./questionnaireApi";

function latestSubmissionFor(questionnaireId: string, submissions: MySubmission[]): MySubmission | undefined {
  return submissions
    .filter((s) => s.questionnaireId === questionnaireId)
    .sort((a, b) => (a.startedAt ?? "").localeCompare(b.startedAt ?? ""))
    .at(-1);
}

export function GuidanceHomePage() {
  const { t } = useTranslation(["questionnaire", "common"]);
  const language = i18n.resolvedLanguage ?? "ro";

  const questionnairesQuery = useQuery({
    queryKey: ["published-questionnaires", language],
    queryFn: () => questionnaireApi.listPublished(language),
  });

  const submissionsQuery = useQuery({
    queryKey: ["my-submissions"],
    queryFn: () => questionnaireApi.listMySubmissions(),
  });

  if (questionnairesQuery.isLoading || submissionsQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3 p-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  if (questionnairesQuery.isError || submissionsQuery.isError) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.generic")} />
      </div>
    );
  }

  const questionnaires = questionnairesQuery.data ?? [];
  const submissions = submissionsQuery.data ?? [];

  return (
    <div className="flex flex-col gap-4 p-4">
      <h1 className="text-lg font-semibold text-text-primary">{t("questionnaire:title")}</h1>
      <CrisisDisclaimer />

      {questionnaires.length === 0 && <EmptyState title={t("questionnaire:noQuestionnaires")} />}

      <div className="flex flex-col gap-2">
        {questionnaires.map((questionnaire) => {
          const submission = latestSubmissionFor(questionnaire.id, submissions);
          const status = submission?.status ?? null;

          const cta =
            status === null || status === undefined
              ? { label: t("questionnaire:cta.start"), to: `/guidance/${questionnaire.id}/fill` }
              : status === "Draft"
                ? { label: t("questionnaire:cta.resume"), to: `/guidance/${questionnaire.id}/fill` }
                : { label: t("questionnaire:cta.view"), to: `/guidance/submissions/${submission!.submissionId}` };

          return (
            <Card key={questionnaire.id} className="flex items-center justify-between gap-3">
              <div>
                <h2 className="text-sm font-semibold text-text-primary">{questionnaire.title}</h2>
                <p className="text-xs text-text-muted">{questionnaire.description}</p>
              </div>
              <div className="flex items-center gap-3">
                {status && (
                  <StatusBadge
                    status={status === "Answered" ? "success" : status === "Submitted" ? "info" : "neutral"}
                    label={t(`questionnaire:submissionStatus.${status}`)}
                  />
                )}
                <Link
                  to={cta.to}
                  className="inline-flex min-h-11 items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-hover"
                >
                  {cta.label}
                </Link>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
