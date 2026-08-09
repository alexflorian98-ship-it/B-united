import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Alert } from "../../../shared/design-system/Alert";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { adminChatApi, ReportResolutionAction } from "./adminChatApi";

/** §53 Chat moderation: Reported Messages, Muted Users, Recent Moderator Actions. */
export function AdminChatModerationPage() {
  const { t } = useTranslation(["admin", "common"]);
  const queryClient = useQueryClient();

  const reportsQuery = useQuery({ queryKey: ["admin-chat-reports"], queryFn: () => adminChatApi.listReports("Open") });
  const mutedQuery = useQuery({ queryKey: ["admin-chat-muted"], queryFn: () => adminChatApi.listMutedUsers() });
  const actionsQuery = useQuery({ queryKey: ["admin-chat-actions"], queryFn: () => adminChatApi.listRecentActions() });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["admin-chat-reports"] });
    void queryClient.invalidateQueries({ queryKey: ["admin-chat-muted"] });
    void queryClient.invalidateQueries({ queryKey: ["admin-chat-actions"] });
  };

  const resolveMutation = useMutation({
    mutationFn: (input: { reportId: string; action: (typeof ReportResolutionAction)[keyof typeof ReportResolutionAction] }) =>
      adminChatApi.resolveReport(input.reportId, input.action, 60, "Reported content"),
    onSuccess: invalidate,
  });

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:chat.title")}</h1>

      <Card className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:chat.reportedMessages")}</h2>
        {reportsQuery.isLoading && <Skeleton className="h-32 w-full" />}
        {reportsQuery.isSuccess && reportsQuery.data.length === 0 && <EmptyState title={t("admin:chat.noReports")} />}
        {reportsQuery.isSuccess &&
          reportsQuery.data.map((report) => (
            <div key={report.reportId} className="flex flex-col gap-2 border-t border-border-default py-3 text-sm">
              <p className="text-text-primary">{report.messageBody ?? t("admin:chat.messageAlreadyRemoved")}</p>
              <p className="text-text-muted">
                {t("admin:chat.author")}: {report.messageAuthorEmail ?? report.messageAuthorUserId} · {t("admin:chat.reportedBy")}: {report.reporterEmail ?? report.reporterUserId}
              </p>
              <p className="text-text-muted">
                {t("admin:chat.reason")}: {report.reason} · {new Date(report.createdAt).toLocaleString()}
              </p>
              <div className="flex flex-wrap gap-2">
                <Button variant="secondary" onClick={() => resolveMutation.mutate({ reportId: report.reportId, action: ReportResolutionAction.Dismiss })}>
                  {t("admin:chat.dismiss")}
                </Button>
                <Button variant="danger" onClick={() => resolveMutation.mutate({ reportId: report.reportId, action: ReportResolutionAction.DeleteMessage })}>
                  {t("admin:chat.deleteMessage")}
                </Button>
                <Button variant="danger" onClick={() => resolveMutation.mutate({ reportId: report.reportId, action: ReportResolutionAction.MuteUser })}>
                  {t("admin:chat.muteUser")}
                </Button>
              </div>
            </div>
          ))}
      </Card>

      <Card className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:chat.mutedUsers")}</h2>
        {mutedQuery.isLoading && <Skeleton className="h-24 w-full" />}
        {mutedQuery.isSuccess && mutedQuery.data.length === 0 && <EmptyState title={t("admin:chat.noMutedUsers")} />}
        {mutedQuery.isSuccess &&
          mutedQuery.data.map((mute) => (
            <div key={mute.muteId} className="flex items-center justify-between border-t border-border-default py-2 text-sm">
              <span className="text-text-primary">{mute.email ?? mute.userId}</span>
              <Badge tone="warning">
                {t("admin:chat.until")} {new Date(mute.expiresAtUtc).toLocaleString()}
              </Badge>
            </div>
          ))}
      </Card>

      <Card className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:chat.recentActions")}</h2>
        {actionsQuery.isLoading && <Skeleton className="h-24 w-full" />}
        {actionsQuery.isSuccess && actionsQuery.data.length === 0 && <Alert tone="info" title={t("admin:chat.noRecentActions")} />}
        {actionsQuery.isSuccess &&
          actionsQuery.data.map((action, index) => (
            <div key={index} className="flex items-center justify-between border-t border-border-default py-2 text-sm">
              <span className="text-text-primary">
                {t(`admin:chat.actionKind.${action.kind}`)} — {action.targetDescription}
              </span>
              <span className="text-text-muted">
                {action.actorEmail ?? "—"} · {new Date(action.occurredAtUtc).toLocaleString()}
              </span>
            </div>
          ))}
      </Card>
    </div>
  );
}
