import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import { expertQuestionnaireApi, type WaitingTimeBucketName } from "./expertQuestionnaireApi";

const BUCKET_STATUS: Record<WaitingTimeBucketName, "success" | "warning" | "danger"> = {
  0: "success",
  1: "warning",
  2: "danger",
};

/** docs/PROMPT.md §25–28/§50: submission queue with an aging indicator, sorted oldest-first by
 * the backend (the metric that matters — "oldest unanswered submission"). */
export function ExpertQueuePage() {
  const { t } = useTranslation(["admin", "common"]);

  const queueQuery = useQuery({
    queryKey: ["expert-queue"],
    queryFn: () => expertQuestionnaireApi.getQueue(),
    refetchInterval: 30_000,
  });

  return (
    <div className="flex flex-col gap-4 p-4">
      <h1 className="text-lg font-semibold text-text-primary">{t("admin:questionnaires.queue.title")}</h1>

      {queueQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {queueQuery.isSuccess && queueQuery.data.length === 0 && <EmptyState title={t("admin:questionnaires.queue.empty")} />}

      {queueQuery.isSuccess && queueQuery.data.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border-default">
          <table className="w-full text-left text-sm">
            <thead className="bg-background text-xs uppercase text-text-muted">
              <tr>
                <th className="px-3 py-2">{t("admin:questionnaires.queue.columns.client")}</th>
                <th className="px-3 py-2">{t("admin:questionnaires.queue.columns.submittedAt")}</th>
                <th className="px-3 py-2">{t("admin:questionnaires.queue.columns.waiting")}</th>
                <th className="px-3 py-2">{t("admin:questionnaires.queue.columns.action")}</th>
              </tr>
            </thead>
            <tbody>
              {queueQuery.data.map((item) => (
                <tr key={item.submissionId} className="border-t border-border-default">
                  <td className="px-3 py-2 font-medium text-text-primary">{item.clientEmail ?? item.userId}</td>
                  <td className="px-3 py-2 text-text-muted">{new Date(item.submittedAt).toLocaleString()}</td>
                  <td className="px-3 py-2">
                    <StatusBadge
                      status={BUCKET_STATUS[item.waitingBucket]}
                      label={t(`admin:questionnaires.queue.buckets.${item.waitingBucket}`)}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <Link to={`/admin/questionnaires/queue/${item.submissionId}`} className="text-sm font-medium text-primary">
                      {t("admin:questionnaires.queue.review")}
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
