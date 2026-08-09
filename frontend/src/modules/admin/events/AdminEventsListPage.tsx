import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import i18n from "../../../shared/i18n/i18n";
import { formatEventDateTime } from "../../events/eventFormatting";
import { adminEventsApi } from "./adminEventsApi";

const STATUS_TONE: Record<string, "success" | "warning" | "danger" | "neutral"> = {
  Draft: "neutral",
  Published: "success",
  Canceled: "danger",
  Completed: "neutral",
};

/** §52 admin event list (Title, Date, Type, Registrations, Capacity, Status, Actions). */
export function AdminEventsListPage() {
  const { t } = useTranslation(["admin", "events", "common"]);
  const language = i18n.resolvedLanguage ?? "ro";

  const listQuery = useQuery({ queryKey: ["admin-events"], queryFn: () => adminEventsApi.listEvents() });

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3 tablet:flex-row tablet:items-center tablet:justify-between">
        <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:events.title")}</h1>
        <Link to="/admin/events/new">
          <Button variant="primary">{t("admin:events.newEvent")}</Button>
        </Link>
      </div>

      {listQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {listQuery.isSuccess && listQuery.data.items.length === 0 && (
        <EmptyState title={t("admin:events.noEvents")} description={t("admin:events.noEventsDescription")} />
      )}

      {listQuery.isSuccess && listQuery.data.items.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border-default bg-surface shadow-sm">
          <table className="w-full text-left text-sm">
            <caption className="sr-only">{t("admin:events.tableCaption")}</caption>
            <thead className="bg-background text-xs uppercase tracking-wide text-text-muted">
              <tr>
                <th scope="col" className="px-3 py-2">{t("admin:events.columns.title")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:events.columns.date")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:events.columns.type")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:events.columns.registrations")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:events.columns.status")}</th>
                <th scope="col" className="px-3 py-2">{t("admin:events.columns.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {listQuery.data.items.map((event) => (
                <tr key={event.id} className="border-t border-border-default">
                  <td className="px-3 py-2 font-medium text-text-primary">{event.title}</td>
                  <td className="px-3 py-2 text-text-muted">{formatEventDateTime(event.startsAtUtc, event.displayTimezone, language)}</td>
                  <td className="px-3 py-2 text-text-muted">{t(`events:locationType.${event.locationType === 0 ? "Online" : "Physical"}`)}</td>
                  <td className="px-3 py-2 text-text-muted">
                    {event.registeredCount}
                    {event.capacity !== null ? ` / ${event.capacity}` : ""}
                  </td>
                  <td className="px-3 py-2">
                    <Badge tone={STATUS_TONE[event.status] ?? "neutral"}>{t(`admin:events.status.${event.status}`)}</Badge>
                  </td>
                  <td className="px-3 py-2">
                    <Link to={`/admin/events/${event.id}`}>
                      <Button variant="secondary">{t("admin:events.view")}</Button>
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
