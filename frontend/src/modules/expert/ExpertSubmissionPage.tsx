import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { expertQuestionnaireApi } from "./expertQuestionnaireApi";

/** docs/PROMPT.md §50: client summary, Q&A cards (not a raw form dump), guidance editor with
 * version history, and the publish action. */
export function ExpertSubmissionPage() {
  const { t } = useTranslation(["admin", "common"]);
  const { submissionId = "" } = useParams<{ submissionId: string }>();
  const queryClient = useQueryClient();
  const [draftBody, setDraftBody] = useState("");
  const [followUpAnswers, setFollowUpAnswers] = useState<Record<string, string>>({});

  const detailQuery = useQuery({
    queryKey: ["expert-submission", submissionId],
    queryFn: () => expertQuestionnaireApi.getSubmission(submissionId),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["expert-submission", submissionId] });

  const latestGuidance = detailQuery.data?.guidanceHistory.at(-1) ?? null;
  const latestIsDraft = latestGuidance !== null && latestGuidance.publishedAt === null;

  useEffect(() => {
    setDraftBody(latestIsDraft ? (latestGuidance?.body ?? "") : "");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [latestGuidance?.id, latestGuidance?.publishedAt]);

  const saveDraftMutation = useMutation({
    mutationFn: () => expertQuestionnaireApi.saveGuidanceDraft(submissionId, draftBody),
    onSuccess: invalidate,
  });

  const publishMutation = useMutation({
    mutationFn: (guidanceId: string) => expertQuestionnaireApi.publishGuidance(guidanceId),
    onSuccess: invalidate,
  });

  const answerFollowUpMutation = useMutation({
    mutationFn: (followUpId: string) => expertQuestionnaireApi.answerFollowUp(followUpId, followUpAnswers[followUpId] ?? ""),
    onSuccess: invalidate,
  });

  if (detailQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3 p-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
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
      <div>
        <h1 className="text-lg font-semibold text-text-primary">{detail.clientEmail ?? detail.userId}</h1>
        {detail.submittedAt && (
          <p className="text-xs text-text-muted">
            {t("admin:questionnaires.queue.columns.submittedAt")}: {new Date(detail.submittedAt).toLocaleString()}
          </p>
        )}
      </div>

      <div className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:questionnaires.detail.answers")}</h2>
        {detail.answers.map((qa) => (
          <Card key={qa.questionId}>
            <p className="text-sm font-medium text-text-primary">{qa.questionText}</p>
            <p className="mt-1 text-sm text-text-secondary">{qa.answerLabels?.join(", ") ?? qa.answerValue}</p>
          </Card>
        ))}
      </div>

      <div className="border-t border-border-default pt-4">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:questionnaires.detail.guidance")}</h2>

        {detail.guidanceHistory
          .filter((g) => g.publishedAt !== null)
          .map((g) => (
            <Card key={g.id} className="mt-2">
              <p className="text-xs text-text-muted">
                {t("admin:questionnaires.detail.versionPublished", { version: g.version, date: new Date(g.publishedAt!).toLocaleString() })}
              </p>
              <p className="mt-2 whitespace-pre-wrap text-sm text-text-secondary">{g.body}</p>
              {g.followUp && (
                <div className="mt-3 border-t border-border-default pt-3">
                  <p className="text-sm font-medium text-text-primary">{t("admin:questionnaires.detail.followUpQuestion")}</p>
                  <p className="text-sm text-text-secondary">{g.followUp.question}</p>
                  {g.followUp.answer ? (
                    <p className="mt-1 text-sm text-text-secondary">{g.followUp.answer}</p>
                  ) : (
                    <div className="mt-2 flex flex-col gap-2">
                      <textarea
                        rows={2}
                        value={followUpAnswers[g.followUp.id] ?? ""}
                        onChange={(e) => setFollowUpAnswers((prev) => ({ ...prev, [g.followUp!.id]: e.target.value }))}
                        className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
                      />
                      <Button
                        variant="secondary"
                        className="self-start"
                        onClick={() => answerFollowUpMutation.mutate(g.followUp!.id)}
                        disabled={answerFollowUpMutation.isPending || !(followUpAnswers[g.followUp.id] ?? "").trim()}
                      >
                        {t("admin:questionnaires.detail.answerFollowUp")}
                      </Button>
                    </div>
                  )}
                </div>
              )}
            </Card>
          ))}

        <Card className="mt-3">
          <p className="text-sm font-medium text-text-primary">
            {latestIsDraft
              ? t("admin:questionnaires.detail.draftVersion", { version: latestGuidance!.version })
              : t("admin:questionnaires.detail.newDraft")}
          </p>
          <textarea
            rows={8}
            value={draftBody}
            onChange={(e) => setDraftBody(e.target.value)}
            className="mt-2 w-full rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
          />
          <div className="mt-3 flex gap-2">
            <Button variant="secondary" onClick={() => saveDraftMutation.mutate()} disabled={saveDraftMutation.isPending || !draftBody.trim()}>
              {t("admin:questionnaires.detail.saveDraft")}
            </Button>
            <Button
              variant="primary"
              onClick={() => latestIsDraft && publishMutation.mutate(latestGuidance!.id)}
              disabled={!latestIsDraft || publishMutation.isPending}
            >
              {t("admin:questionnaires.detail.publish")}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
