import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams } from "react-router-dom";
import i18n from "../../shared/i18n/i18n";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { IconButton } from "../../shared/design-system/IconButton";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { sanitizeRichTextHtml } from "../../shared/forms/sanitizeHtml";
import { progressApi } from "../progress/progressApi";
import { contentApi, type ClientContentItem, type ClientSection } from "./contentApi";
import { YouTubePlayer } from "./YouTubePlayer";

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

  if (programQuery.isLoading) {
    return (
      <div className="p-4">
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  if (programQuery.isError || !programQuery.data || !current) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.notFound")} />
      </div>
    );
  }

  const progressByItemId = new Map(progressQuery.data?.map((p) => [p.contentItemId, p]) ?? []);
  const currentProgress = progressByItemId.get(current.item.id);

  const curriculum = (
    <nav aria-label={t("content:curriculum")} className="flex flex-col gap-3 p-4">
      {programQuery.data.sections.map((section) => (
        <div key={section.id}>
          <h3 className="text-xs font-semibold uppercase text-text-muted">{section.title}</h3>
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
                    className={`flex min-h-11 items-center gap-2 rounded-md px-2 text-sm ${
                      isCurrent ? "bg-background font-medium text-primary" : "text-text-secondary hover:bg-background"
                    }`}
                  >
                    <span aria-hidden="true">{itemProgress?.status === "Completed" ? "✓" : "○"}</span>
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
    <div className="flex min-h-screen flex-col desktop:flex-row">
      <aside className="hidden w-72 shrink-0 border-r border-border-default bg-surface desktop:block">{curriculum}</aside>

      <div className="flex flex-1 flex-col">
        <div className="flex items-center justify-between border-b border-border-default bg-surface p-2 desktop:hidden">
          <IconButton label={t("content:openCurriculum")} onClick={() => setIsDrawerOpen(true)}>
            <span aria-hidden="true">☰</span>
          </IconButton>
          <span className="text-sm font-medium text-text-primary">{current.section.title}</span>
        </div>

        <main className="flex-1 p-4">
          <h1 className="text-lg font-semibold text-text-primary">{current.item.title}</h1>

          <div className="mt-4">
            {current.item.type === "Video" &&
              (playbackQuery.isSuccess ? (
                <YouTubePlayer
                  videoId={extractYouTubeIdFromEmbedUrl(playbackQuery.data.playbackUrl)}
                  resumeFromSeconds={currentProgress?.lastVideoPositionSeconds ?? undefined}
                  onProgress={(positionSeconds, watchPercentage) => recordVideoProgress.mutate({ positionSeconds, watchPercentage })}
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
        </main>

        <footer className="flex items-center justify-between border-t border-border-default bg-surface p-4">
          <Button variant="secondary" disabled={!previous} onClick={() => previous && navigate(`/programs/${slug}/learn/${previous.item.id}`)}>
            {t("common:actions.back")}
          </Button>
          <Button variant="primary" disabled={!next} onClick={() => next && navigate(`/programs/${slug}/learn/${next.item.id}`)}>
            {t("content:next")}
          </Button>
        </footer>
      </div>

      {isDrawerOpen && (
        <div className="fixed inset-0 z-50 flex desktop:hidden">
          <div aria-hidden="true" onClick={() => setIsDrawerOpen(false)} className="fixed inset-0 bg-text-primary/30" />
          <div className="relative flex w-72 flex-col bg-surface">
            <IconButton label={t("common:actions.close")} className="self-end m-2" onClick={() => setIsDrawerOpen(false)}>
              <span aria-hidden="true">×</span>
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
