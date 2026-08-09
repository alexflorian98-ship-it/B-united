import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { primaryButtonLinkClassName } from "../../../shared/design-system/linkAsButton";
import { adminContentApi, ProgramStatus, type ProgramStatusValue } from "./adminContentApi";

const TABS: { key: string; value: ProgramStatusValue | null }[] = [
  { key: "all", value: null },
  { key: "draft", value: ProgramStatus.Draft },
  { key: "published", value: ProgramStatus.Published },
  { key: "archived", value: ProgramStatus.Archived },
];

const STATUS_LABEL_KEY: Record<ProgramStatusValue, string> = {
  [ProgramStatus.Draft]: "admin:content.status.draft",
  [ProgramStatus.Published]: "admin:content.status.published",
  [ProgramStatus.Archived]: "admin:content.status.archived",
};

export function AdminProgramListPage() {
  const { t } = useTranslation(["admin", "content", "common"]);
  const [status, setStatus] = useState<ProgramStatusValue | null>(null);

  const programsQuery = useQuery({
    queryKey: ["admin-programs", status],
    queryFn: () => adminContentApi.listPrograms(status, 1, 50),
  });

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3 tablet:flex-row tablet:items-center tablet:justify-between">
        <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:content.programs")}</h1>
        <Link to="/admin/programs/new" className={primaryButtonLinkClassName}>
          {t("admin:content.newProgram")}
        </Link>
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
            {t(`admin:content.tabs.${tab.key}`)}
          </button>
        ))}
      </div>

      {programsQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {programsQuery.isSuccess && programsQuery.data.items.length === 0 && (
        <EmptyState title={t("admin:content.noPrograms")} description={t("admin:content.noProgramsDescription")} />
      )}

      {programsQuery.isSuccess && programsQuery.data.items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border-default bg-surface shadow-sm">
          <table className="w-full text-left text-sm">
            <caption className="sr-only">{t("admin:content.tableCaption")}</caption>
            <thead className="bg-background text-xs uppercase tracking-wide text-text-muted">
              <tr>
                <th scope="col" className="px-3 py-2">{t("admin:content.columns.title")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:content.columns.sections")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:content.columns.languages")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:content.columns.status")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:content.columns.updated")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:content.columns.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {programsQuery.data.items.map((program) => (
                <tr key={program.id} className="border-t border-border-default">
                  <td className="px-3 py-2 font-medium text-text-primary">{program.title}</td>
                  <td className="px-3 py-2">{program.sectionCount}</td>
                  <td className="px-3 py-2">{program.languages.join(", ") || "—"}</td>
                  <td className="px-3 py-2">
                    <Badge tone={program.status === ProgramStatus.Published ? "success" : program.status === ProgramStatus.Archived ? "neutral" : "warning"}>
                      {t(STATUS_LABEL_KEY[program.status])}
                    </Badge>
                  </td>
                  <td className="px-3 py-2 text-text-muted">{new Date(program.updatedAt).toLocaleDateString()}</td>
                  <td className="px-3 py-2">
                    <Link to={`/admin/programs/${program.id}`}>
                      <Button variant="secondary">{t("admin:content.edit")}</Button>
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
