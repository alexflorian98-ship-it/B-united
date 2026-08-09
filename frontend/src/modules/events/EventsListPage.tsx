import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Alert } from "../../shared/design-system/Alert";
import { Badge } from "../../shared/design-system/Badge";
import { Card } from "../../shared/design-system/Card";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { Icon } from "../../shared/design-system/Icon";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import i18n from "../../shared/i18n/i18n";
import { eventsApi, type EventRegistrationStatusName } from "./eventsApi";
import { formatEventDateTime } from "./eventFormatting";

const REGISTRATION_TONE: Record<EventRegistrationStatusName, "success" | "warning" | "neutral"> = {
  Registered: "success",
  Waitlisted: "warning",
  Canceled: "neutral",
};

/** P5.11.a — event listing with upcoming/past filter (§41-44). */
export function EventsListPage() {
  const { t } = useTranslation(["events", "common"]);
  const language = i18n.resolvedLanguage ?? "ro";
  const [includePast, setIncludePast] = useState(false);

  const eventsQuery = useQuery({
    queryKey: ["events", includePast, language],
    queryFn: () => eventsApi.listEvents(includePast, language),
  });

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("events:title")}</h1>

      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => setIncludePast(false)}
          className={`min-h-11 rounded-full border px-4 text-sm font-medium transition-colors duration-150 ${
            !includePast
              ? "border-primary bg-primary text-on-primary"
              : "border-border-strong bg-surface text-text-secondary hover:border-primary hover:text-primary"
          }`}
        >
          {t("events:tabs.upcoming")}
        </button>
        <button
          type="button"
          onClick={() => setIncludePast(true)}
          className={`min-h-11 rounded-full border px-4 text-sm font-medium transition-colors duration-150 ${
            includePast
              ? "border-primary bg-primary text-on-primary"
              : "border-border-strong bg-surface text-text-secondary hover:border-primary hover:text-primary"
          }`}
        >
          {t("events:tabs.past")}
        </button>
      </div>

      {eventsQuery.isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}

      {eventsQuery.isError && <Alert tone="danger" title={t("common:errors.generic")} />}

      {eventsQuery.isSuccess && eventsQuery.data.length === 0 && (
        <EmptyState title={t("events:noEvents")} description={t("events:noEventsDescription")} />
      )}

      {eventsQuery.isSuccess && eventsQuery.data.length > 0 && (
        <div className="flex flex-col gap-3">
          {eventsQuery.data.map((event) => (
            <Link key={event.id} to={`/events/${event.id}`}>
              <Card className="flex items-start gap-4 transition-shadow duration-150 hover:shadow-md">
                <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-accent/15 text-accent">
                  <Icon name="calendar" />
                </span>
                <div className="flex-1">
                  <div className="flex items-start justify-between gap-2">
                    <h2 className="font-serif text-lg font-medium text-text-primary">{event.title}</h2>
                    {event.myRegistrationStatus && (
                      <StatusBadge status={REGISTRATION_TONE[event.myRegistrationStatus]} label={t(`events:registrationStatus.${event.myRegistrationStatus}`)} />
                    )}
                  </div>
                  <p className="mt-1 text-sm text-text-secondary">{formatEventDateTime(event.startsAtUtc, event.displayTimezone, language)}</p>
                  <div className="mt-2 flex items-center gap-2">
                    <Badge>{t(`events:locationType.${event.locationType === 0 ? "Online" : "Physical"}`)}</Badge>
                    {event.capacity !== null && (
                      <span className="text-xs text-text-muted">
                        {event.registeredCount}/{event.capacity} {t("events:registered")}
                      </span>
                    )}
                  </div>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
