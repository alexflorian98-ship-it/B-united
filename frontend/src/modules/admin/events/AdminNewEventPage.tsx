import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { Alert } from "../../../shared/design-system/Alert";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { applyApiErrorToForm } from "../../../shared/forms/applyApiErrorToForm";
import { zonedInputValueToUtcIso } from "../../events/eventFormatting";
import { adminEventsApi } from "./adminEventsApi";
import { EVENT_TIMEZONES } from "./eventTimezones";

const schema = z
  .object({
    defaultLanguage: z.enum(["ro", "en"]),
    title: z.string().min(1),
    description: z.string().min(1),
    startsAt: z.string().min(1),
    endsAt: z.string().min(1),
    displayTimezone: z.string().min(1),
    locationType: z.enum(["0", "1"]),
    location: z.string().optional(),
    meetingUrl: z.string().optional(),
    capacity: z.string().optional(),
  })
  .refine((v) => v.locationType === "1" || (v.meetingUrl?.trim().length ?? 0) > 0, {
    message: "required",
    path: ["meetingUrl"],
  })
  .refine((v) => v.locationType === "0" || (v.location?.trim().length ?? 0) > 0, {
    message: "required",
    path: ["location"],
  });
type FormValues = z.infer<typeof schema>;

/** P5.15: event editor — creation half (translations, date/time, timezone, location, capacity). */
export function AdminNewEventPage() {
  const { t } = useTranslation(["admin", "events", "common"]);
  const navigate = useNavigate();

  const { register, handleSubmit, watch, setError } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { defaultLanguage: "ro", displayTimezone: "Europe/Bucharest", locationType: "0" },
  });
  const [formError, setFormError] = useState<string | null>(null);
  const locationType = watch("locationType");

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      adminEventsApi.createEvent({
        defaultLanguage: values.defaultLanguage,
        title: values.title,
        description: values.description,
        startsAtUtc: zonedInputValueToUtcIso(values.startsAt, values.displayTimezone),
        endsAtUtc: zonedInputValueToUtcIso(values.endsAt, values.displayTimezone),
        displayTimezone: values.displayTimezone,
        locationType: values.locationType === "1" ? 1 : 0,
        location: values.locationType === "1" ? (values.location ?? null) : null,
        meetingUrl: values.locationType === "0" ? (values.meetingUrl ?? null) : null,
        capacity: values.capacity ? Number(values.capacity) : null,
      }),
    onSuccess: (eventId) => navigate(`/admin/events/${eventId}`, { replace: true }),
    onError: (error: unknown) =>
      setFormError(applyApiErrorToForm(error, setError, t, ["defaultLanguage", "title", "description", "displayTimezone", "location", "meetingUrl", "capacity"])),
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  return (
    <div>
      <Card className="max-w-lg">
        <h1 className="text-2xl font-semibold text-text-primary">{t("admin:events.newEvent")}</h1>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          {formError && <Alert tone="danger" title={formError} />}

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.defaultLanguage")}</span>
            <select {...register("defaultLanguage")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
              <option value="ro">{t("common:language.ro")}</option>
              <option value="en">{t("common:language.en")}</option>
            </select>
          </label>

          <Input label={t("admin:events.fields.title")} {...register("title")} />

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.description")}</span>
            <textarea rows={4} {...register("description")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary" />
          </label>

          <div className="grid grid-cols-2 gap-3">
            <Input type="datetime-local" label={t("admin:events.fields.startsAt")} {...register("startsAt")} />
            <Input type="datetime-local" label={t("admin:events.fields.endsAt")} {...register("endsAt")} />
          </div>

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.displayTimezone")}</span>
            <select {...register("displayTimezone")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
              {EVENT_TIMEZONES.map((tz) => (
                <option key={tz} value={tz}>
                  {tz}
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:events.fields.locationType")}</span>
            <select {...register("locationType")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
              <option value="0">{t("events:locationType.Online")}</option>
              <option value="1">{t("events:locationType.Physical")}</option>
            </select>
          </label>

          {locationType === "0" && <Input label={t("admin:events.fields.meetingUrl")} {...register("meetingUrl")} />}
          {locationType === "1" && <Input label={t("admin:events.fields.location")} {...register("location")} />}

          <Input type="number" min={1} label={t("admin:events.fields.capacity")} hint={t("admin:events.fields.capacityHint")} {...register("capacity")} />

          <Button type="submit" variant="primary" disabled={mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
