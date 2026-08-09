/** Formats an event's UTC start/end time in its own DisplayTimezone (never the browser's local
 * zone or a hand-built string) per docs/DEVELOPMENT_INSTRUCTIONS.md §8. */
export function formatEventDateTime(isoUtc: string, displayTimezone: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, {
    timeZone: displayTimezone,
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(isoUtc));
}

/** Converts a `<input type="datetime-local">` value (a naive wall-clock string, e.g.
 * "2026-08-20T18:00") interpreted as a moment in `timeZone` into a UTC ISO string, without
 * pulling in a timezone library — no npm timezone package is a project dependency yet.
 * Standard trick: read the same instant in both UTC and the target zone, and use the
 * difference as the zone's current offset. */
export function zonedInputValueToUtcIso(dateTimeLocalValue: string, timeZone: string): string {
  const asIfUtc = new Date(`${dateTimeLocalValue}:00.000Z`);
  const zoned = new Date(asIfUtc.toLocaleString("en-US", { timeZone }));
  const offsetMs = asIfUtc.getTime() - zoned.getTime();
  return new Date(asIfUtc.getTime() + offsetMs).toISOString();
}

/** The inverse of {@link zonedInputValueToUtcIso} — formats a UTC ISO instant back into a
 * `<input type="datetime-local">`-compatible wall-clock string in `timeZone`. */
export function utcIsoToZonedInputValue(isoUtc: string, timeZone: string): string {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).formatToParts(new Date(isoUtc));

  const get = (type: string) => parts.find((p) => p.type === type)?.value ?? "00";
  return `${get("year")}-${get("month")}-${get("day")}T${get("hour")}:${get("minute")}`;
}
