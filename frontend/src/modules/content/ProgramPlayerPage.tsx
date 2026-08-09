import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams } from "react-router-dom";
import i18n from "../../shared/i18n/i18n";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Icon } from "../../shared/design-system/Icon";
import { IconButton } from "../../shared/design-system/IconButton";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { sanitizeRichTextHtml } from "../../shared/forms/sanitizeHtml";
import { resolveMessageKey } from "../../shared/forms/applyApiErrorToForm";
import { progressApi } from "../progress/progressApi";
import { contentApi, type ClientContentItem, type ClientSection, type QuizAnswerInput, type QuizSubmissionResult } from "./contentApi";
import { YouTubePlayer } from "./YouTubePlayer";
import { ApiError } from "../../shared/api/apiError";

interface FlatItem {
  section: ClientSection;
  item: ClientContentItem;
}

export function ProgramPlayerPage() {
  const { t } = useTranslation(["content", "common"]);
  const { slug = "", contentItemId = "" } = useParams<{ slug: string; contentItemId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const language = i18n.resolvedLanguage ?? "ro";
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [videoPlaybackFailed, setVideoPlaybackFailed] = useState(false);
  const [quizAnswers, setQuizAnswers] = useState<Record<string, string>>({});
  const [quizResult, setQuizResult] = useState<QuizSubmissionResult | null>(null);
  const [quizSubmitError, setQuizSubmitError] = useState<ApiError | null>(null);

  const programQuery = useQuery({
    queryKey: ["published-program", slug, language],
    queryFn: () => contentApi.getProgram(slug, language),
  });

  const flatItems: FlatItem[] = useMemo(
    () => (programQuery.data?.sections.flatMap((section) => section.items.map((item) => ({ section, item }))) ?? []),
    [programQuery.data],
  );
  const allContentItemIds = useMemo(() => flatItems.map((f) => f.item.id), [flatItems]);

  const progressQuery = useQuery({
    queryKey: ["content-progress", allContentItemIds],
    queryFn: () => progressApi.getContentProgress(allContentItemIds),
    enabled: allContentItemIds.length > 0,
  });

  const currentIndex = flatItems.findIndex((f) => f.item.id === contentItemId);
  const current = flatItems[currentIndex];
  const previous = currentIndex > 0 ? flatItems[currentIndex - 1] : undefined;
  const next = currentIndex >= 0 && currentIndex < flatItems.length - 1 ? flatItems[currentIndex + 1] : undefined;

  useEffect(() => {
    setVideoPlaybackFailed(false);
    setQuizAnswers({});
    setQuizResult(null);
    setQuizSubmitError(null);
  }, [contentItemId]);

  const playbackQuery = useQuery({
    queryKey: ["video-playback", current?.item.id],
    queryFn: () => contentApi.getVideoPlayback(current!.item.id),
    enabled: !!current && current.item.type === "Video",
  });

  const invalidateProgress = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-progress"] });
    void queryClient.invalidateQueries({ queryKey: ["section-progress"] });
  };

  const recordVideoProgress = useMutation({
    mutationFn: (input: { positionSeconds: number; watchPercentage: number }) =>
      progressApi.recordVideoPosition({
        contentItemId: current!.item.id,
        sectionId: current!.section.id,
        sectionContentItemIds: current!.section.items.map((i) => i.id),
        positionSeconds: input.positionSeconds,
        watchPercentage: input.watchPercentage,
      }),
    onSuccess: invalidateProgress,
  });

  const markCompleted = useMutation({
    mutationFn: () =>
      progressApi.markCompleted({
        contentItemId: current!.item.id,
        sectionId: current!.section.id,
        sectionContentItemIds: current!.section.items.map((i) => i.id),
      }),
    onSuccess: invalidateProgress,
  });

  const submitQuiz = useMutation({
    mutationFn: (answers: QuizAnswerInput[]) => contentApi.submitQuiz(current!.item.id, answers),
    onSuccess: (result) => {
      setQuizResult(result);
      setQuizSubmitError(null);
      markCompleted.mutate();
    },
    onError: (error) => {
      setQuizSubmitError(error instanceof ApiError ? error : null);
    },
  });

  if (programQuery.isLoading) {
    return (
      <div className="bg-background p-4">
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  if (programQuery.isError || !programQuery.data || !current) {
    return (
      <div className="bg-background p-4">
        <Alert tone="danger" title={t("common:errors.notFound")} />
      </div>
    );
  }

  const accessRequired = programQuery.data.ownershipState !== "Owned" ||
    (playbackQuery.error instanceof ApiError && playbackQuery.error.code === "PROGRAM_ACCESS_REQUIRED") ||
    (progressQuery.error instanceof ApiError && progressQuery.error.code === "PROGRAM_ACCESS_REQUIRED");
  if (accessRequired) {
    return <div className="flex min-h-screen items-center justify-center bg-background p-4"><Card className="max-w-lg text-center">
      <h1 className="font-serif text-2xl font-medium text-text-primary">{t("content:paywall.title")}</h1>
      <p className="mt-2 text-sm text-text-secondary">{t("content:paywall.description")}</p>
      <Link to={`/programs/${slug}`} className="mt-5 inline-flex min-h-11 items-center rounded-full bg-primary px-5 text-sm font-medium text-on-primary">{t("content:paywall.viewOffer")}</Link>
    </Card></div>;
  }

  const progressByItemId = new Map(progressQuery.data?.map((p) => [p.contentItemId, p]) ?? []);
  const currentProgress = progressByItemId.get(current.item.id);

  const quizQuestions = current.item.type === "Quiz" ? (current.item.quizQuestions ?? []) : [];
  const allQuizQuestionsAnswered = quizQuestions.length > 0 && quizQuestions.every((q) => quizAnswers[q.id]);
  const quizErrorMessage = quizSubmitError
    ? quizSubmitError.code === "PROGRAM_ACCESS_REQUIRED"
      ? t("content:paywall.title")
      : t(resolveMessageKey(quizSubmitError.messageKey))
    : null;

  const curriculum = (
    <nav aria-label={t("content:curriculum")} className="flex flex-col gap-3 p-4">
      {programQuery.data.sections.map((section) => (
        <div key={section.id}>
          <h3 className="text-xs font-semibold uppercase tracking-wide text-text-muted">{section.title}</h3>
          <ul className="mt-1 flex flex-col gap-1">
            {section.items.map((item) => {
              const itemProgress = progressByItemId.get(item.id);
              const isCurrent = item.id === current.item.id;
              return (
                <li key={item.id}>
                  <Link
                    to={`/programs/${slug}/learn/${item.id}`}
                    onClick={() => setIsDrawerOpen(false)}
                    aria-current={isCurrent ? "page" : undefined}
                    className={`flex min-h-11 items-center gap-2 rounded-lg px-2 text-sm ${
                      isCurrent ? "bg-primary/10 font-medium text-primary" : "text-text-secondary hover:bg-background"
                    }`}
                  >
                    {itemProgress?.status === "Completed" ? (
                      <Icon name="check" size={16} className="shrink-0 text-success" />
                    ) : (
                      <span aria-hidden="true" className="h-3 w-3 shrink-0 rounded-full border border-border-strong" />
                    )}
                    {item.title}
                  </Link>
                </li>
              );
            })}
          </ul>
        </div>
      ))}
    </nav>
  );

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-background desktop:flex-row">
      <aside className="hidden w-72 shrink-0 overflow-y-auto border-r border-border-default bg-surface desktop:block">
        <Link
          to={`/programs/${slug}`}
          className="flex min-h-11 items-center gap-2 border-b border-border-default px-4 text-sm font-medium text-text-secondary transition-colors duration-150 hover:text-primary"
        >
          <Icon name="close" size={16} />
          {t("content:exitProgram")}
        </Link>
        {curriculum}
      </aside>

      <div className="flex min-h-0 flex-1 flex-col">
        <div className="flex shrink-0 items-center gap-2 border-b border-border-default bg-surface p-2 desktop:hidden">
          <IconButton label={t("content:openCurriculum")} onClick={() => setIsDrawerOpen(true)}>
            <Icon name="menu" />
          </IconButton>
          <span className="flex-1 text-sm font-medium text-text-primary">{current.section.title}</span>
          <Link to={`/programs/${slug}`} aria-label={t("content:exitProgram")} className="flex min-h-11 min-w-11 items-center justify-center rounded-md text-text-secondary hover:bg-background hover:text-text-primary">
            <Icon name="close" />
          </Link>
        </div>

        <main className="flex-1 overflow-y-auto p-4 tablet:p-8">
          <h1 className="font-serif text-2xl font-medium text-text-primary">{current.item.title}</h1>

          <div className="mt-4">
            {current.item.type === "Video" &&
              (playbackQuery.isSuccess ? (
                <YouTubePlayer
                  videoId={extractYouTubeIdFromEmbedUrl(playbackQuery.data.playbackUrl)}
                  resumeFromSeconds={currentProgress?.lastVideoPositionSeconds ?? undefined}
                  onProgress={(positionSeconds, watchPercentage) => recordVideoProgress.mutate({ positionSeconds, watchPercentage })}
                  errorMessage={t("content:videoPlaybackError")}
                  retryLabel={t("common:actions.retry")}
                  watchOnYouTubeLabel={t("content:watchOnYouTube")}
                  onPlaybackError={() => setVideoPlaybackFailed(true)}
                />
              ) : (
                <Skeleton className="aspect-video w-full" />
              ))}

            {current.item.type === "RichText" && (
              <div
                className="max-w-none text-sm leading-relaxed text-text-primary [&_a]:text-primary [&_a]:underline [&_h1]:text-lg [&_h1]:font-semibold [&_h2]:text-base [&_h2]:font-semibold [&_p]:mb-3 [&_ul]:list-disc [&_ul]:pl-5"
                // eslint-disable-next-line react/no-danger -- sanitized just above via DOMPurify
                dangerouslySetInnerHTML={{ __html: sanitizeRichTextHtml(current.item.body ?? "") }}
              />
            )}

            {current.item.type === "Quiz" && (
              <div className="flex flex-col gap-6">
                {quizQuestions.length === 0 && (
                  <Alert tone="warning" title={t("content:quiz.noQuestions")} />
                )}

                {quizQuestions.map((question) => {
                  const questionResult = quizResult?.perQuestionResults.find((r) => r.questionId === question.id);
                  return (
                    <fieldset key={question.id} className="flex flex-col gap-1" disabled={!!quizResult}>
                      <legend className="text-sm font-medium text-text-primary">{question.text}</legend>
                      <div className="mt-2 flex flex-col gap-1">
                        {question.options.map((option) => {
                          const isSelected = quizAnswers[question.id] === option.id;
                          const isCorrectOption = questionResult?.correctOptionId === option.id;
                          const optionToneClass = quizResult
                            ? isCorrectOption
                              ? "text-success font-medium"
                              : isSelected
                                ? "text-danger"
                                : "text-text-secondary"
                            : "text-text-secondary";
                          return (
                            <label key={option.id} className={`flex min-h-11 items-center gap-2 text-sm ${optionToneClass}`}>
                              <input
                                type="radio"
                                name={`quiz-question-${question.id}`}
                                value={option.id}
                                checked={isSelected}
                                onChange={() => setQuizAnswers((prev) => ({ ...prev, [question.id]: option.id }))}
                                disabled={!!quizResult}
                                className="h-4 w-4 accent-primary"
                              />
                              {option.label}
                            </label>
                          );
                        })}
                      </div>
                      {questionResult && (
                        <p className={`mt-1 text-xs font-medium ${questionResult.wasCorrect ? "text-success" : "text-danger"}`}>
                          {questionResult.wasCorrect ? t("content:quiz.correct") : t("content:quiz.incorrect")}
                        </p>
                      )}
                    </fieldset>
                  );
                })}

                {quizErrorMessage && <Alert tone="danger" title={quizErrorMessage} />}

                {quizResult ? (
                  <div className="flex flex-col items-start gap-3">
                    <p className="text-sm font-medium text-text-primary">
                      {t("content:quiz.score", { correct: quizResult.correctCount, total: quizResult.totalQuestions })}
                    </p>
                    <Button
                      variant="secondary"
                      onClick={() => {
                        setQuizAnswers({});
                        setQuizResult(null);
                        setQuizSubmitError(null);
                      }}
                    >
                      {t("content:quiz.retake")}
                    </Button>
                  </div>
                ) : (
                  quizQuestions.length > 0 && (
                    <div className="flex flex-col items-start gap-2">
                      <Button
                        variant="primary"
                        disabled={!allQuizQuestionsAnswered || submitQuiz.isPending}
                        onClick={() =>
                          submitQuiz.mutate(
                            quizQuestions.map((q) => ({ questionId: q.id, selectedOptionId: quizAnswers[q.id] })),
                          )
                        }
                      >
                        {submitQuiz.isPending ? t("content:quiz.submitting") : t("content:quiz.submit")}
                      </Button>
                      {!allQuizQuestionsAnswered && (
                        <p className="text-xs text-text-muted">{t("content:quiz.selectAllHint")}</p>
                      )}
                    </div>
                  )
                )}
              </div>
            )}
          </div>

          {current.item.type === "RichText" && (
            <Button
              variant={currentProgress?.status === "Completed" ? "secondary" : "primary"}
              className="mt-4"
              onClick={() => markCompleted.mutate()}
              disabled={markCompleted.isPending || currentProgress?.status === "Completed"}
            >
              {currentProgress?.status === "Completed" ? t("content:completed") : t("content:markCompleted")}
            </Button>
          )}
          {current.item.type === "Video" && videoPlaybackFailed && (
            <Button
              variant={currentProgress?.status === "Completed" ? "secondary" : "primary"}
              className="mt-4"
              onClick={() => markCompleted.mutate()}
              disabled={markCompleted.isPending || currentProgress?.status === "Completed"}
            >
              {currentProgress?.status === "Completed" ? t("content:completed") : t("content:markVideoCompletedFallback")}
            </Button>
          )}
        </main>

        <footer className="flex shrink-0 items-center justify-between border-t border-border-default bg-surface p-4">
          <Button variant="secondary" disabled={!previous} onClick={() => previous && navigate(`/programs/${slug}/learn/${previous.item.id}`)}>
            {t("common:actions.back")}
          </Button>
          <Button variant="primary" onClick={() => next ? navigate(`/programs/${slug}/learn/${next.item.id}`) : navigate(`/programs/${slug}`)}>
            {next ? t("content:next") : t("content:finishProgram")}
          </Button>
        </footer>
      </div>

      {isDrawerOpen && (
        <div className="fixed inset-0 z-50 flex desktop:hidden">
          <div aria-hidden="true" onClick={() => setIsDrawerOpen(false)} className="fixed inset-0 bg-text-primary/30" />
          <div className="relative flex w-72 flex-col bg-surface">
            <IconButton label={t("common:actions.close")} className="self-end m-2" onClick={() => setIsDrawerOpen(false)}>
              <Icon name="close" />
            </IconButton>
            {curriculum}
          </div>
        </div>
      )}
    </div>
  );
}

function extractYouTubeIdFromEmbedUrl(embedUrl: string): string {
  const match = /\/embed\/([a-zA-Z0-9_-]{11})/.exec(embedUrl);
  return match?.[1] ?? "";
}
