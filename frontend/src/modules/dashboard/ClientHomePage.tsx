import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Card } from "../../shared/design-system/Card";
import { Icon } from "../../shared/design-system/Icon";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import { useCurrentUser } from "../../shared/auth/useCurrentUser";
import i18n from "../../shared/i18n/i18n";
import { eventsApi } from "../events/eventsApi";
import { formatEventDateTime } from "../events/eventFormatting";

/**
 * Phase 1's client home screen: an account greeting/state and a link to the one destination
 * that actually exists yet (Profile). Deliberately does not show progress/program
 * summaries — those modules don't have a dashboard read model yet, and inventing placeholder
 * data for them would violate "no placeholder business data" (P1.41.c). The "upcoming event"
 * card (P5.13.a, §41) is the one exception: it's a real, narrow read against Events.
 */
export function ClientHomePage() {
  const { t } = useTranslation(["dashboard", "events", "common"]);
  const user = useCurrentUser();
  const language = i18n.resolvedLanguage ?? "ro";

  const upcomingEventQuery = useQuery({
    queryKey: ["my-upcoming-event", language],
    queryFn: () => eventsApi.getMyUpcoming(language),
  });

  return (
    <div className="flex flex-col gap-6">
      <div className="relative overflow-hidden rounded-lg bg-primary p-5 text-on-primary shadow-md">
        <div className="relative flex flex-col gap-3">
          <h1 className="text-2xl font-semibold text-on-primary tablet:text-3xl">
            {t("dashboard:greeting", { email: user?.email ?? "" })}
          </h1>
          <div>
            <StatusBadge status="success" label={t("dashboard:accountVerified")} />
          </div>
          <p className="max-w-xl text-sm text-on-primary/80">{t("dashboard:phase1Notice")}</p>
          <Link
            to="/profile"
            className="mt-2 inline-flex w-fit items-center gap-1.5 rounded-full bg-accent px-5 py-2.5 text-sm font-medium text-on-accent transition-colors duration-150 hover:bg-accent-hover"
          >
            {t("common:nav.profile")}
            <Icon name="chevron-right" size={16} />
          </Link>
        </div>
      </div>

      {upcomingEventQuery.data && (
        <Link to={`/events/${upcomingEventQuery.data.eventId}`} className="block">
          <Card className="flex items-start gap-4 transition-shadow duration-150 hover:shadow-md">
            <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-accent/15 text-accent">
              <Icon name="calendar" />
            </span>
            <div className="flex-1">
              <h2 className="text-xs font-semibold uppercase tracking-wide text-text-muted">
                {t("dashboard:upcomingEvent")}
              </h2>
              <p className="mt-1 font-serif text-lg font-medium text-text-primary">
                {upcomingEventQuery.data.eventTitle}
              </p>
              <p className="mt-1 text-sm text-text-secondary">
                {formatEventDateTime(upcomingEventQuery.data.startsAtUtc, upcomingEventQuery.data.displayTimezone, language)}
              </p>
              <div className="mt-2">
                <StatusBadge
                  status={upcomingEventQuery.data.status === "Registered" ? "success" : "warning"}
                  label={t(`events:registrationStatus.${upcomingEventQuery.data.status}`)}
                />
              </div>
            </div>
            <Icon name="chevron-right" className="mt-2 shrink-0 text-text-muted" />
          </Card>
        </Link>
      )}
    </div>
  );
}
