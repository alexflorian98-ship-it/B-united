import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Navigate, useParams } from "react-router-dom";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { CrisisDisclaimer } from "../../shared/crisis/CrisisDisclaimer";
import { sanitizeRichTextHtml } from "../../shared/forms/sanitizeHtml";
import { questionnaireApi } from "./questionnaireApi";

export function SubmissionStatusPage() {
  const { t } = useTranslation(["questionnaire", "common"]);
  const { submissionId = "" } = useParams<{ submissionId: string }>();
  const queryClient = useQueryClient();
  const [followUpQuestion, setFollowUpQuestion] = useState("");

  const submissionQuery = useQuery({
    queryKey: ["my-submission", submissionId],
    queryFn: () => questionnaireApi.getSubmission(submissionId),
  });

  const guidanceQuery = useQuery({
    queryKey: ["questionnaire", submissionId],
    queryFn: () => questionnaireApi.getGuidance(submissionId),
    enabled: submissionQuery.data?.status === "Answered",
  });

  const followUpMutation = useMutation({
    mutationFn: () => questionnaireApi.submitFollowUp(guidanceQuery.data!.id, followUpQuestion),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["questionnaire", submissionId] }),
  });

  if (submissionQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  if (!submissionQuery.data) {
    return <Alert tone="danger" title={t("common:errors.notFound")} />;
  }

  const submission = submissionQuery.data;

  if (submission.status === "Draft") {
    return <Navigate to={`/guidance/${submission.questionnaireId}/fill`} replace />;
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("questionnaire:title")}</h1>
      <CrisisDisclaimer />

      {submission.status === "Submitted" && (
        <Card>
          <p className="font-serif text-lg font-medium text-text-primary">{t("questionnaire:underReview.title")}</p>
          <p className="mt-2 text-sm text-text-secondary">{t("questionnaire:underReview.body")}</p>
        </Card>
      )}

      {submission.status === "Answered" && guidanceQuery.data && (
        <Card>
          <p className="text-xs uppercase tracking-wide text-text-muted">
            {t("questionnaire:response.versionLabel", { version: guidanceQuery.data.version })}
          </p>
          <div
            className="prose prose-sm mt-3 max-w-none text-text-primary [&_a]:text-primary [&_h1]:font-serif [&_h2]:font-serif"
            // eslint-disable-next-line react/no-danger
            dangerouslySetInnerHTML={{ __html: sanitizeRichTextHtml(guidanceQuery.data.body) }}
          />

          <div className="mt-6 border-t border-border-default pt-4">
            {guidanceQuery.data.followUp ? (
              <div className="flex flex-col gap-2">
                <p className="text-sm font-medium text-text-primary">{t("questionnaire:followUp.yourQuestion")}</p>
                <p className="text-sm text-text-secondary">{guidanceQuery.data.followUp.question}</p>
                {guidanceQuery.data.followUp.answer ? (
                  <>
                    <p className="mt-2 text-sm font-medium text-text-primary">{t("questionnaire:followUp.expertAnswer")}</p>
                    <p className="text-sm text-text-secondary">{guidanceQuery.data.followUp.answer}</p>
                  </>
                ) : (
                  <p className="text-xs text-text-muted">{t("questionnaire:followUp.awaitingAnswer")}</p>
                )}
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                <label htmlFor="follow-up-question" className="text-sm font-medium text-text-primary">
                  {t("questionnaire:followUp.askOne")}
                </label>
                <textarea
                  id="follow-up-question"
                  value={followUpQuestion}
                  onChange={(e) => setFollowUpQuestion(e.target.value)}
                  rows={3}
                  className="rounded-lg border border-border-default bg-surface px-3 py-2 text-sm text-text-primary placeholder:text-text-muted"
                />
                <Button
                  className="self-start"
                  onClick={() => followUpMutation.mutate()}
                  disabled={followUpMutation.isPending || followUpQuestion.trim().length === 0}
                >
                  {t("questionnaire:followUp.submit")}
                </Button>
              </div>
            )}
          </div>
        </Card>
      )}
    </div>
  );
}
