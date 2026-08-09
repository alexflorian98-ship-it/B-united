import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { primaryButtonLinkClassName } from "../../../shared/design-system/linkAsButton";
import { adminQuestionnaireApi, type QuestionnaireStatusName } from "./adminQuestionnaireApi";

const TABS: { key: string; value: QuestionnaireStatusName | null }[] = [
  { key: "all", value: null },
  { key: "draft", value: "Draft" },
  { key: "published", value: "Published" },
  { key: "archived", value: "Archived" },
];

export function AdminQuestionnaireListPage() {
  const { t } = useTranslation(["admin", "common"]);
  const [status, setStatus] = useState<QuestionnaireStatusName | null>(null);

  const listQuery = useQuery({
    queryKey: ["admin-questionnaires", status],
    queryFn: () => adminQuestionnaireApi.list(status ?? undefined),
  });

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3 tablet:flex-row tablet:items-center tablet:justify-between">
        <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:questionnaires.title")}</h1>
        <div className="flex flex-wrap gap-2">
          <Link to="/admin/questionnaires/queue" className={primaryButtonLinkClassName}>
            {t("admin:questionnaires.viewQueue")}
          </Link>
          <Link to="/admin/questionnaires/new" className={primaryButtonLinkClassName}>
            {t("admin:questionnaires.newQuestionnaire")}
          </Link>
        </div>
      </div>

      <div className="flex flex-wrap gap-2" role="tablist">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            role="tab"
            aria-selected={status === tab.value}
            onClick={() => setStatus(tab.value)}
            className={`min-h-11 rounded-full border px-4 text-sm font-medium transition-colors duration-150 ${
              status === tab.value
                ? "border-primary bg-primary text-on-primary"
                : "border-border-strong bg-surface text-text-secondary hover:border-primary hover:text-primary"
            }`}
          >
            {t(`admin:questionnaires.tabs.${tab.key}`)}
          </button>
        ))}
      </div>

      {listQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {listQuery.isSuccess && listQuery.data.items.length === 0 && (
        <EmptyState title={t("admin:questionnaires.noQuestionnaires")} description={t("admin:questionnaires.noQuestionnairesDescription")} />
      )}

      {listQuery.isSuccess && listQuery.data.items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border-default bg-surface shadow-sm">
          <table className="w-full text-left text-sm">
            <caption className="sr-only">{t("admin:questionnaires.tableCaption")}</caption>
            <thead className="bg-background text-xs uppercase tracking-wide text-text-muted">
              <tr>
                <th scope="col" className="px-3 py-2">{t("admin:questionnaires.columns.title")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:questionnaires.columns.questions")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:questionnaires.columns.languages")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:questionnaires.columns.status")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:questionnaires.columns.updated")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:questionnaires.columns.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {listQuery.data.items.map((questionnaire) => (
                <tr key={questionnaire.id} className="border-t border-border-default">
                  <td className="px-3 py-2 font-medium text-text-primary">{questionnaire.title}</td>
                  <td className="px-3 py-2">{questionnaire.questionCount}</td>
                  <td className="px-3 py-2">{questionnaire.languages.join(", ") || "—"}</td>
                  <td className="px-3 py-2">
                    <Badge tone={questionnaire.status === "Published" ? "success" : questionnaire.status === "Archived" ? "neutral" : "warning"}>
                      {t(`admin:questionnaires.status.${questionnaire.status.toLowerCase()}`)}
                    </Badge>
                  </td>
                  <td className="px-3 py-2 text-text-muted">{new Date(questionnaire.updatedAt).toLocaleDateString()}</td>
                  <td className="px-3 py-2">
                    <Link to={`/admin/questionnaires/${questionnaire.id}`}>
                      <Button variant="secondary">{t("admin:questionnaires.edit")}</Button>
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
