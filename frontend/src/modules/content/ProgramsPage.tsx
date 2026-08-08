import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import i18n from "../../shared/i18n/i18n";
import { Badge } from "../../shared/design-system/Badge";
import { Card } from "../../shared/design-system/Card";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { contentApi } from "./contentApi";

export function ProgramsPage() {
  const { t } = useTranslation(["content", "common"]);
  const [domainId, setDomainId] = useState<string | null>(null);
  const language = i18n.resolvedLanguage ?? "ro";

  const domainsQuery = useQuery({ queryKey: ["content-domains"], queryFn: contentApi.listDomains });
  const programsQuery = useQuery({
    queryKey: ["published-programs", domainId, language],
    queryFn: () => contentApi.listPrograms(domainId, language),
  });

  const domains = useMemo(() => domainsQuery.data ?? [], [domainsQuery.data]);

  return (
    <div className="flex flex-col gap-4 p-4">
      <h1 className="text-lg font-semibold text-text-primary">{t("common:nav.programs")}</h1>

      <div className="flex flex-wrap gap-2" role="tablist" aria-label={t("content:domainFilter")}>
        <button
          type="button"
          role="tab"
          aria-selected={domainId === null}
          onClick={() => setDomainId(null)}
          className={`min-h-11 rounded-full border px-4 text-sm font-medium ${
            domainId === null ? "border-primary bg-primary text-white" : "border-border-default text-text-secondary"
          }`}
        >
          {t("content:allDomains")}
        </button>
        {domains.map((domain) => (
          <button
            key={domain.id}
            type="button"
            role="tab"
            aria-selected={domainId === domain.id}
            onClick={() => setDomainId(domain.id)}
            className={`min-h-11 rounded-full border px-4 text-sm font-medium ${
              domainId === domain.id ? "border-primary bg-primary text-white" : "border-border-default text-text-secondary"
            }`}
          >
            {t(`content:domains.${domain.slug}`)}
          </button>
        ))}
      </div>

      {programsQuery.isLoading && (
        <div className="grid grid-cols-1 gap-4 tablet:grid-cols-2 desktop:grid-cols-3">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      )}

      {programsQuery.isSuccess && programsQuery.data.length === 0 && (
        <EmptyState title={t("content:noPrograms")} description={t("content:noProgramsDescription")} />
      )}

      {programsQuery.isSuccess && programsQuery.data.length > 0 && (
        <div className="grid grid-cols-1 gap-4 tablet:grid-cols-2 desktop:grid-cols-3">
          {programsQuery.data.map((program) => (
            <Link key={program.id} to={`/programs/${program.slug}`}>
              <Card className="flex h-full flex-col gap-2 transition-shadow hover:shadow-md">
                <Badge tone="info">{t(`content:domains.${domains.find((d) => d.id === program.domainId)?.slug ?? ""}`)}</Badge>
                <h2 className="text-base font-semibold text-text-primary">{program.title}</h2>
                <p className="text-sm text-text-secondary">{program.shortDescription}</p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
