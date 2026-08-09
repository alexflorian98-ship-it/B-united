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
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("common:nav.programs")}</h1>

      <div className="flex flex-wrap gap-2" role="tablist" aria-label={t("content:domainFilter")}>
        <button
          type="button"
          role="tab"
          aria-selected={domainId === null}
          onClick={() => setDomainId(null)}
          className={`min-h-11 rounded-full border px-4 text-sm font-medium transition-colors duration-150 ${
            domainId === null
              ? "border-primary bg-primary text-on-primary"
              : "border-border-strong bg-surface text-text-secondary hover:border-primary hover:text-primary"
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
            className={`min-h-11 rounded-full border px-4 text-sm font-medium transition-colors duration-150 ${
              domainId === domain.id
                ? "border-primary bg-primary text-on-primary"
                : "border-border-strong bg-surface text-text-secondary hover:border-primary hover:text-primary"
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
              <Card className="flex h-full flex-col gap-2 transition-shadow duration-150 hover:shadow-md">
                <Badge tone="info" className="w-fit">
                  {t(`content:domains.${domains.find((d) => d.id === program.domainId)?.slug ?? ""}`)}
                </Badge>
                <h2 className="font-serif text-lg font-medium text-text-primary">{program.title}</h2>
                <p className="text-sm text-text-secondary">{program.shortDescription}</p>
                <p className="mt-auto pt-2 text-sm font-semibold text-primary">
                  {program.ownershipState === "Owned"
                    ? t("content:owned")
                    : program.activeOffer
                      ? t("content:price", { amount: program.activeOffer.amount.toFixed(2), currency: program.activeOffer.currency })
                      : t("content:unavailable")}
                </p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
