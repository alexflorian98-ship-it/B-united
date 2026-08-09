import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Alert } from "../../shared/design-system/Alert";
import { Badge } from "../../shared/design-system/Badge";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import i18n from "../../shared/i18n/i18n";
import { eventsApi, type EventRegistrationStatusName } from "./eventsApi";
import { formatEventDateTime } from "./eventFormatting";

const REGISTRATION_TONE: Record<EventRegistrationStatusName, "success" | "warning" | "neutral"> = {
  Registered: "success",
  Waitlisted: "warning",
  Canceled: "neutral",
};

/** P5.11.b — event detail with registration status and capacity/waitlist info; P5.12 —
 * register/cancel actions with immediate status feedback. */
export function EventDetailPage() {
  const { t } = useTranslation(["events", "common"]);
  const { eventId = "" } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const language = i18n.resolvedLanguage ?? "ro";
  const queryClient = useQueryClient();

  const eventQuery = useQuery({
    queryKey: ["event", eventId, language],
    queryFn: () => eventsApi.getEvent(eventId, language),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["event", eventId] });
    void queryClient.invalidateQueries({ queryKey: ["events"] });
    void queryClient.invalidateQueries({ queryKey: ["my-upcoming-event"] });
  };

  const registerMutation = useMutation({
    mutationFn: () => eventsApi.register(eventId),
    onSuccess: invalidate,
  });

  const cancelMutation = useMutation({
    mutationFn: () => eventsApi.cancelRegistration(eventId),
    onSuccess: invalidate,
  });

  if (eventQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  if (eventQuery.isError || !eventQuery.data) {
    return <Alert tone="danger" title={t("common:errors.notFound")} />;
  }

  const event = eventQuery.data;
  const isRegistered = event.myRegistrationStatus === "Registered" || event.myRegistrationStatus === "Waitlisted";
  const registrationClosed = event.status !== "Published";
  const mutationError = registerMutation.error ?? cancelMutation.error;

  return (
    <div className="flex flex-col gap-4">
      <Button variant="secondary" className="self-start" onClick={() => navigate("/events")}>
        {t("common:actions.back")}
      </Button>

      <Card className="flex flex-col gap-3">
        <div className="flex items-start justify-between gap-2">
          <h1 className="font-serif text-2xl font-medium text-text-primary">{event.title}</h1>
          {event.myRegistrationStatus && (
            <StatusBadge status={REGISTRATION_TONE[event.myRegistrationStatus]} label={t(`events:registrationStatus.${event.myRegistrationStatus}`)} />
          )}
        </div>

        <p className="text-sm text-text-secondary">{formatEventDateTime(event.startsAtUtc, event.displayTimezone, language)}</p>

        <div className="flex flex-wrap items-center gap-2">
          <Badge>{t(`events:locationType.${event.locationType === 0 ? "Online" : "Physical"}`)}</Badge>
          {event.location && <span className="text-sm text-text-secondary">{event.location}</span>}
          {event.capacity !== null && (
            <span className="text-xs text-text-muted">
              {event.registeredCount}/{event.capacity} {t("events:registered")}
              {event.waitlistedCount > 0 && ` · ${event.waitlistedCount} ${t("events:waitlisted")}`}
            </span>
          )}
        </div>

        <p className="text-sm leading-relaxed text-text-primary">{event.description}</p>

        {isRegistered && event.myRegistrationStatus === "Registered" && event.meetingUrl && (
          <a href={event.meetingUrl} target="_blank" rel="noreferrer" className="text-sm font-medium text-primary hover:underline">
            {t("events:joinLink")}
          </a>
        )}

        {mutationError && <Alert tone="danger" title={t("common:errors.generic")} />}

        <div className="flex gap-2">
          {!isRegistered && (
            <Button onClick={() => registerMutation.mutate()} disabled={registerMutation.isPending || registrationClosed}>
              {t("events:register")}
            </Button>
          )}
          {isRegistered && (
            <Button variant="danger" onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending}>
              {t("events:cancelRegistration")}
            </Button>
          )}
        </div>

        {registerMutation.isSuccess && (
          <Alert
            tone={registerMutation.data.status === "Registered" ? "success" : "info"}
            title={t(`events:registerFeedback.${registerMutation.data.status}`)}
          />
        )}
      </Card>
    </div>
  );
}
