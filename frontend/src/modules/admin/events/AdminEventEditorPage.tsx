import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Alert } from "../../../shared/design-system/Alert";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { formatEventDateTime, utcIsoToZonedInputValue, zonedInputValueToUtcIso } from "../../events/eventFormatting";
import { adminEventsApi, type EventLocationTypeValue } from "./adminEventsApi";
import { EVENT_TIMEZONES } from "./eventTimezones";

const STATUS_TONE: Record<string, "success" | "warning" | "danger" | "neutral"> = {
  Draft: "neutral",
  Published: "success",
  Canceled: "danger",
  Completed: "neutral",
};

/** P5.15/P5.16: event editor (schedule/location/capacity/translations/publication) plus detail
 * (registered users, waitlist, reminders). Combined into one page — the same pattern the admin
 * Billing subscription detail screen uses for a single-entity admin view. */
export function AdminEventEditorPage() {
  const { t } = useTranslation(["admin", "events", "common"]);
  const { eventId = "" } = useParams<{ eventId: string }>();
  const queryClient = useQueryClient();

  const detailQuery = useQuery({ queryKey: ["admin-event", eventId], queryFn: () => adminEventsApi.getEvent(eventId) });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["admin-event", eventId] });
    void queryClient.invalidateQueries({ queryKey: ["admin-events"] });
  };

  const [translationLanguage, setTranslationLanguage] = useState<"ro" | "en">("ro");
  const [translationTitle, setTranslationTitle] = useState("");
  const [translationDescription, setTranslationDescription] = useState("");

  const [startsAt, setStartsAt] = useState("");
  const [endsAt, setEndsAt] = useState("");
  const [displayTimezone, setDisplayTimezone] = useState("Europe/Bucharest");
  const [locationType, setLocationType] = useState<EventLocationTypeValue>(0);
  const [location, setLocation] = useState("");
  const [meetingUrl, setMeetingUrl] = useState("");
  const [capacity, setCapacity] = useState("");
  const [programIds, setProgramIds] = useState("");

  useEffect(() => {
    if (!detailQuery.data) return;
    const event = detailQuery.data;
    setDisplayTimezone(event.displayTimezone);
    setStartsAt(utcIsoToZonedInputValue(event.startsAtUtc, event.displayTimezone));
    setEndsAt(utcIsoToZonedInputValue(event.endsAtUtc, event.displayTimezone));
    setLocationType(event.locationType);
    setLocation(event.location ?? "");
    setMeetingUrl(event.meetingUrl ?? "");
    setCapacity(event.capacity !== null ? String(event.capacity) : "");
    setProgramIds(event.programIds.join(", "));

    const existing = event.translations.find((tr) => tr.language === translationLanguage);
    setTranslationTitle(existing?.title ?? "");
    setTranslationDescription(existing?.description ?? "");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detailQuery.data, translationLanguage]);

  const translationMutation = useMutation({
    mutationFn: () => adminEventsApi.upsertTranslation(eventId, translationLanguage, translationTitle, translationDescription),
    onSuccess: invalidate,
  });

  const scheduleMutation = useMutation({
    mutationFn: () =>
      adminEventsApi.updateSchedule(eventId, {
        startsAtUtc: zonedInputValueToUtcIso(startsAt, displayTimezone),
        endsAtUtc: zonedInputValueToUtcIso(endsAt, displayTimezone),
        displayTimezone,
        locationType,
        location: locationType === 1 ? location : null,
        meetingUrl: locationType === 0 ? meetingUrl : null,
        capacity: capacity ? Number(capacity) : null,
      }),
    onSuccess: invalidate,
  });

  const publishMutation = useMutation({ mutationFn: () => adminEventsApi.publish(eventId), onSuccess: invalidate });
  const programsMutation = useMutation({
    mutationFn: () => adminEventsApi.setPrograms(eventId, programIds.split(",").map((id) => id.trim()).filter(Boolean)),
    onSuccess: invalidate,
  });
  const cancelMutation = useMutation({ mutationFn: () => adminEventsApi.cancel(eventId), onSuccess: invalidate });

  if (detailQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (detailQuery.isError || !detailQuery.data) {
    return <Alert tone="danger" title={t("common:errors.generic")} />;
  }

  const event = detailQuery.data;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3 tablet:flex-row tablet:items-center tablet:justify-between">
        <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{event.translations.find((tr) => tr.language === event.defaultLanguage)?.title}</h1>
        <div className="flex items-center gap-2">
          <Badge tone={STATUS_TONE[event.status] ?? "neutral"}>{t(`admin:events.status.${event.status}`)}</Badge>
          {event.status === "Draft" && (
            <Button variant="primary" onClick={() => publishMutation.mutate()} disabled={publishMutation.isPending}>
              {t("admin:events.publish")}
            </Button>
          )}
          {(event.status === "Draft" || event.status === "Published") && (
            <Button variant="danger" onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending}>
              {t("admin:events.cancel")}
            </Button>
          )}
        </div>
      </div>

      <Card className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:events.schedule")}</h2>
        <div className="grid grid-cols-2 gap-3">
          <Input type="datetime-local" label={t("admin:events.fields.startsAt")} value={startsAt} onChange={(e) => setStartsAt(e.target.value)} />
          <Input type="datetime-local" label={t("admin:events.fields.endsAt")} value={endsAt} onChange={(e) => setEndsAt(e.target.value)} />
        </div>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.displayTimezone")}</span>
          <select value={displayTimezone} onChange={(e) => setDisplayTimezone(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
            {EVENT_TIMEZONES.map((tz) => (
              <option key={tz} value={tz}>
                {tz}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.locationType")}</span>
          <select
            value={locationType}
            onChange={(e) => setLocationType(Number(e.target.value) as EventLocationTypeValue)}
            className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
          >
            <option value={0}>{t("events:locationType.Online")}</option>
            <option value={1}>{t("events:locationType.Physical")}</option>
          </select>
        </label>
        {locationType === 0 && <Input label={t("admin:events.fields.meetingUrl")} value={meetingUrl} onChange={(e) => setMeetingUrl(e.target.value)} />}
        {locationType === 1 && <Input label={t("admin:events.fields.location")} value={location} onChange={(e) => setLocation(e.target.value)} />}
        <Input type="number" min={1} label={t("admin:events.fields.capacity")} value={capacity} onChange={(e) => setCapacity(e.target.value)} />
        <Button variant="secondary" className="self-start" onClick={() => scheduleMutation.mutate()} disabled={scheduleMutation.isPending}>
          {t("common:actions.save")}
        </Button>
      </Card>

      <Card className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:events.programAccess")}</h2>
        <Input label={t("admin:events.fields.programIds")} value={programIds} onChange={(e) => setProgramIds(e.target.value)} />
        <p className="text-xs text-text-muted">{t("admin:events.fields.programIdsHint")}</p>
        <Button variant="secondary" className="self-start" onClick={() => programsMutation.mutate()} disabled={programsMutation.isPending}>{t("common:actions.save")}</Button>
      </Card>

      <Card className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:events.translations")}</h2>
        <div className="flex gap-2">
          {(["ro", "en"] as const).map((lang) => (
            <button
              key={lang}
              type="button"
              onClick={() => setTranslationLanguage(lang)}
              className={`min-h-11 rounded-full px-4 text-sm font-medium transition-colors duration-150 ${translationLanguage === lang ? "bg-primary text-on-primary" : "bg-background text-text-secondary hover:text-primary"}`}
            >
              {t(`common:language.${lang}`)}
            </button>
          ))}
        </div>
        <Input label={t("admin:events.fields.title")} value={translationTitle} onChange={(e) => setTranslationTitle(e.target.value)} />
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.description")}</span>
          <textarea
            rows={4}
            value={translationDescription}
            onChange={(e) => setTranslationDescription(e.target.value)}
            className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
          />
        </label>
        <Button variant="secondary" className="self-start" onClick={() => translationMutation.mutate()} disabled={translationMutation.isPending}>
          {t("common:actions.save")}
        </Button>
      </Card>

      <Card className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">
          {t("admin:events.registeredUsers")} ({event.registrations.length})
        </h2>
        {event.registrations.length === 0 && <p className="text-sm text-text-muted">{t("admin:events.noRegistrations")}</p>}
        {event.registrations.map((r) => (
          <div key={r.registrationId} className="flex items-center justify-between border-t border-border-default py-2 text-sm">
            <span className="text-text-primary">{r.email ?? r.userId}</span>
            <span className="text-text-muted">{new Date(r.registeredAt).toLocaleString()}</span>
          </div>
        ))}
      </Card>

      <Card className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">
          {t("admin:events.waitlist")} ({event.waitlist.length})
        </h2>
        {event.waitlist.length === 0 && <p className="text-sm text-text-muted">{t("admin:events.noWaitlist")}</p>}
        {event.waitlist.map((r) => (
          <div key={r.registrationId} className="flex items-center justify-between border-t border-border-default py-2 text-sm">
            <span className="text-text-primary">{r.email ?? r.userId}</span>
            <span className="text-text-muted">{new Date(r.registeredAt).toLocaleString()}</span>
          </div>
        ))}
      </Card>

      <Card className="flex flex-col gap-2">
        <h2 className="text-sm font-semibold text-text-primary">{t("admin:events.reminders")}</h2>
        {event.reminders.length === 0 && <p className="text-sm text-text-muted">{t("admin:events.noReminders")}</p>}
        {event.reminders.map((reminder, index) => (
          <div key={index} className="flex items-center justify-between border-t border-border-default py-2 text-sm">
            <span className="text-text-primary">
              {reminder.email ?? "—"} · {t(`admin:events.reminderType.${reminder.type}`)}
            </span>
            <span className="text-text-muted">
              {formatEventDateTime(reminder.scheduledForUtc, displayTimezone, "ro")} —{" "}
              {reminder.sentAtUtc ? t("admin:events.sent") : t("admin:events.notSent")}
            </span>
          </div>
        ))}
      </Card>
    </div>
  );
}
