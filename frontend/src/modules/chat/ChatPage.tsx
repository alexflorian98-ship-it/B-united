import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Icon } from "../../shared/design-system/Icon";
import { Modal } from "../../shared/design-system/Modal";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { chatApi } from "./chatApi";

const POLL_INTERVAL_MS = 5000;

/** P6.04.b — polling instead of SignalR (explicitly acceptable per docs/PROMPT.md §33-34: "do
 * not delay release for real-time perfection"). P6.12-P6.15: room switcher, paginated message
 * list with pinned-message highlight, persistent privacy notice, report action. */
export function ChatPage() {
  const { t } = useTranslation(["chat", "common"]);
  const queryClient = useQueryClient();
  const [activeRoom, setActiveRoom] = useState<string | null>(null);
  const [draft, setDraft] = useState("");
  const [reportTarget, setReportTarget] = useState<string | null>(null);
  const [reportReason, setReportReason] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const roomsQuery = useQuery({ queryKey: ["chat-rooms"], queryFn: () => chatApi.listRooms(), refetchInterval: POLL_INTERVAL_MS });
  const messagesQuery = useQuery({
    queryKey: ["chat-messages", activeRoom],
    queryFn: () => chatApi.getMessages(activeRoom!),
    enabled: activeRoom !== null,
    refetchInterval: POLL_INTERVAL_MS,
  });

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messagesQuery.data]);

  useEffect(() => {
    if (activeRoom) void chatApi.markRead(activeRoom).then(() => queryClient.invalidateQueries({ queryKey: ["chat-rooms"] }));
  }, [activeRoom, queryClient]);

  const sendMutation = useMutation({
    mutationFn: (body: string) => chatApi.sendMessage(activeRoom!, body),
    onSuccess: () => {
      setDraft("");
      void queryClient.invalidateQueries({ queryKey: ["chat-messages", activeRoom] });
      void queryClient.invalidateQueries({ queryKey: ["chat-rooms"] });
    },
  });

  const reportMutation = useMutation({
    mutationFn: () => chatApi.reportMessage(reportTarget!, reportReason),
    onSuccess: () => {
      setReportTarget(null);
      setReportReason("");
    },
  });

  const onSend = () => {
    if (draft.trim().length === 0) return;
    sendMutation.mutate(draft);
  };

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("common:nav.community")}</h1>
      <div className="flex h-[70vh] min-h-[420px] flex-col rounded-xl border border-border-default bg-surface shadow-sm desktop:flex-row">
      <aside className="flex w-full shrink-0 gap-2 overflow-x-auto border-b border-border-default bg-surface p-3 desktop:w-56 desktop:flex-col desktop:overflow-visible desktop:border-b-0 desktop:border-r">
        {roomsQuery.data?.map((room) => {
          return (
            <button
              key={room.roomId}
              type="button"
              onClick={() => room.hasAccess && setActiveRoom(room.roomId)}
              disabled={!room.hasAccess}
              className={`flex min-h-11 shrink-0 items-center justify-between gap-2 rounded-full px-4 text-sm font-medium transition-colors duration-150 ${
                activeRoom === room.roomId ? "bg-primary text-on-primary" : room.hasAccess ? "text-text-secondary hover:bg-background" : "cursor-not-allowed text-text-muted opacity-60"
              }`}
            >
              {room.name} {!room.hasAccess && `· ${t("chat:locked")}`}
              {room.unreadCount > 0 && activeRoom !== room.roomId && room.hasAccess && (
                <span className="rounded-full bg-accent px-2 py-0.5 text-xs font-semibold text-on-accent">{room.unreadCount}</span>
              )}
            </button>
          );
        })}
      </aside>

      <div className="flex flex-1 flex-col">
        <div className="border-b border-border-default p-3">
          <Alert tone="info" title={t("chat:privacyNotice.title")}>
            {t("chat:privacyNotice.body")}
          </Alert>
        </div>

        <div className="flex-1 overflow-y-auto p-4">
          {!activeRoom && <Alert tone="info" title={t("chat:selectRoom")} />}
          {messagesQuery.isLoading && activeRoom && <Skeleton className="h-64 w-full" />}

          {messagesQuery.isSuccess && (
            <div className="flex flex-col gap-2">
              {messagesQuery.data.items
                .slice()
                .sort((a, b) => (a.isPinned === b.isPinned ? 0 : a.isPinned ? -1 : 1))
                .map((message) => (
                  <div
                    key={message.id}
                    className={`rounded-lg border p-3 text-sm ${message.isPinned ? "border-accent/50 bg-accent/5" : "border-border-default bg-surface"}`}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-medium text-text-primary">{message.email ?? t("chat:unknownUser")}</span>
                      <div className="flex items-center gap-2">
                        {message.isPinned && <span className="text-xs font-medium text-accent">{t("chat:pinned")}</span>}
                        <span className="text-xs text-text-muted">{new Date(message.createdAt).toLocaleTimeString()}</span>
                      </div>
                    </div>
                    <p className="mt-1 text-text-secondary">{message.isDeleted ? t("chat:messageDeleted") : message.body}</p>
                    {!message.isDeleted && (
                      <button type="button" onClick={() => setReportTarget(message.id)} className="mt-1 text-xs text-danger hover:underline">
                        {t("chat:report")}
                      </button>
                    )}
                  </div>
                ))}
              <div ref={messagesEndRef} />
            </div>
          )}
        </div>

        <div className="flex items-center gap-2 border-t border-border-default p-3">
          <input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && onSend()}
            placeholder={t("chat:messagePlaceholder")}
            aria-label={t("chat:messagePlaceholder")}
            className="min-h-11 flex-1 rounded-full border border-border-default bg-surface px-4 py-2 text-sm text-text-primary placeholder:text-text-muted"
          />
          <Button onClick={onSend} disabled={!activeRoom || sendMutation.isPending || draft.trim().length === 0}>
            <Icon name="chevron-right" size={16} />
            {t("chat:send")}
          </Button>
        </div>
      </div>
      </div>

      <Modal open={reportTarget !== null} onClose={() => setReportTarget(null)} title={t("chat:reportModal.title")}>
        <div className="flex flex-col gap-3">
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("chat:reportModal.reason")}</span>
            <select
              value={reportReason}
              onChange={(e) => setReportReason(e.target.value)}
              className="rounded-lg border border-border-default bg-surface px-3 py-2 text-sm text-text-primary"
            >
              <option value="">—</option>
              <option value="spam">{t("chat:reportModal.reasons.spam")}</option>
              <option value="harassment">{t("chat:reportModal.reasons.harassment")}</option>
              <option value="sensitiveInfo">{t("chat:reportModal.reasons.sensitiveInfo")}</option>
              <option value="other">{t("chat:reportModal.reasons.other")}</option>
            </select>
          </label>
          <Button onClick={() => reportMutation.mutate()} disabled={reportMutation.isPending || reportReason.length === 0}>
            {t("chat:reportModal.submit")}
          </Button>
        </div>
      </Modal>
    </div>
  );
}
