import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import i18n from "../../shared/i18n/i18n";
import { ApiError } from "../../shared/api/apiError";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { CrisisDisclaimer } from "../../shared/crisis/CrisisDisclaimer";
import { QuestionInput } from "./QuestionInput";
import { questionnaireApi, type MySubmission } from "./questionnaireApi";

export function QuestionnaireFillPage() {
  const { t } = useTranslation(["questionnaire", "common"]);
  const { questionnaireId = "" } = useParams<{ questionnaireId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const language = i18n.resolvedLanguage ?? "ro";

  const [needsConsent, setNeedsConsent] = useState(false);
  const [submissionId, setSubmissionId] = useState<string | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const startMutation = useMutation({
    mutationFn: () => questionnaireApi.start(questionnaireId),
    onSuccess: (id) => {
      setSubmissionId(id);
      setNeedsConsent(false);
    },
    onError: (err: unknown) => {
      if (ApiError.isApiError(err) && err.code === "QUESTIONNAIRE_CONSENT_REQUIRED") {
        setNeedsConsent(true);
      } else {
        setError(t("common:errors.generic"));
      }
    },
  });

  // React 19 StrictMode double-invokes this effect in dev, which would fire two concurrent
  // `start` calls; if they resolve out of order, a stale failed call's onError can clobber the
  // successful retry's state (same bug class as SessionProvider's single-flight refresh fix —
  // see docs/HANDOVER.md bug #12). Guarding with a ref so only the first invocation per
  // questionnaireId actually starts a request.
  const hasStartedRef = useRef<string | null>(null);
  useEffect(() => {
    if (hasStartedRef.current === questionnaireId) return;
    hasStartedRef.current = questionnaireId;
    startMutation.mutate();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [questionnaireId]);

  const consentMutation = useMutation({
    mutationFn: () => questionnaireApi.recordConsent(),
    onSuccess: () => startMutation.mutate(),
  });

  const questionnaireQuery = useQuery({
    queryKey: ["client-questionnaire", questionnaireId, language],
    queryFn: () => questionnaireApi.getQuestionnaire(questionnaireId, language),
    enabled: Boolean(submissionId),
  });

  const submissionQuery = useQuery({
    queryKey: ["my-submission", submissionId],
    queryFn: () => questionnaireApi.getSubmission(submissionId!),
    enabled: Boolean(submissionId),
  });

  useEffect(() => {
    if (submissionQuery.data) {
      setAnswers(Object.fromEntries(submissionQuery.data.answers.map((a) => [a.questionId, a.value])));
    }
  }, [submissionQuery.data]);

  const saveDraftMutation = useMutation({
    mutationFn: () =>
      questionnaireApi.saveDraftAnswers(
        submissionId!,
        Object.entries(answers).map(([questionId, value]) => ({ questionId, value })),
      ),
  });

  const submitMutation = useMutation({
    mutationFn: async () => {
      await questionnaireApi.saveDraftAnswers(
        submissionId!,
        Object.entries(answers).map(([questionId, value]) => ({ questionId, value })),
      );
      await questionnaireApi.submit(submissionId!);
    },
    onSuccess: () => {
      // Both this page and SubmissionStatusPage key their submission query as
      // ["my-submission", submissionId]. Merely invalidating leaves a window where the cache is
      // still marked stale-but-present, and React Query's stale-while-revalidate default would
      // still hand SubmissionStatusPage the OLD "Draft" value on its very first render — which
      // triggers its Draft -> fill redirect and silently starts a second submission. Writing the
      // known-correct value directly closes that window instead of racing a background refetch.
      queryClient.setQueryData<MySubmission>(["my-submission", submissionId], (previous) =>
        previous ? { ...previous, status: "Submitted", submittedAt: new Date().toISOString() } : previous,
      );
      queryClient.invalidateQueries({ queryKey: ["my-submissions"] });
      navigate(`/guidance/submissions/${submissionId}`);
    },
    onError: (err: unknown) => {
      setError(
        ApiError.isApiError(err) && err.code === "QUESTIONNAIRE_REQUIRED_ANSWERS_MISSING"
          ? t("questionnaire:fill.requiredAnswersMissing")
          : t("common:errors.generic"),
      );
    },
  });

  if (needsConsent) {
    return (
      <Card className="mx-auto max-w-xl">
        <h1 className="text-2xl font-semibold text-text-primary">{t("questionnaire:consent.title")}</h1>
        <p className="mt-2 text-sm text-text-secondary">{t("questionnaire:consent.body")}</p>
        <Button className="mt-4" onClick={() => consentMutation.mutate()} disabled={consentMutation.isPending}>
          {t("questionnaire:consent.agree")}
        </Button>
      </Card>
    );
  }

  if (startMutation.isPending || questionnaireQuery.isLoading || submissionQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  if (!questionnaireQuery.data) {
    return <Alert tone="danger" title={t("common:errors.notFound")} />;
  }

  const questionnaire = questionnaireQuery.data;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{questionnaire.title}</h1>
        <p className="mt-1 text-sm text-text-secondary">{questionnaire.description}</p>
      </div>
      <CrisisDisclaimer />

      {error && <Alert tone="danger" title={error} />}

      <div className="flex flex-col gap-4">
        {questionnaire.questions.map((question) => (
          <Card key={question.id}>
            <QuestionInput
              question={question}
              value={answers[question.id] ?? ""}
              onChange={(value) => setAnswers((prev) => ({ ...prev, [question.id]: value }))}
            />
          </Card>
        ))}
      </div>

      <div className="flex gap-3">
        <Button variant="secondary" onClick={() => saveDraftMutation.mutate()} disabled={saveDraftMutation.isPending}>
          {t("questionnaire:fill.saveDraft")}
        </Button>
        <Button onClick={() => submitMutation.mutate()} disabled={submitMutation.isPending}>
          {t("questionnaire:fill.submit")}
        </Button>
      </div>
      {saveDraftMutation.isSuccess && !submitMutation.isPending && (
        <p className="text-xs text-text-muted">{t("questionnaire:fill.draftSaved")}</p>
      )}
    </div>
  );
}
