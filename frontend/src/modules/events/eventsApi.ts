import { apiRequest } from "../../shared/api/apiClient";

export type EventLocationTypeName = "Online" | "Physical";
export type EventStatusName = "Draft" | "Published" | "Canceled" | "Completed";
export type EventRegistrationStatusName = "Registered" | "Waitlisted" | "Canceled";

// EventLocationType binds via the JSON request body, where the backend's raw C# enum requires a
// numeric value (no global string-enum converter is configured).
export const EventLocationType = { Online: 0, Physical: 1 } as const;
export type EventLocationTypeValue = (typeof EventLocationType)[keyof typeof EventLocationType];

export interface EventSummary {
  id: string;
  title: string;
  startsAtUtc: string;
  endsAtUtc: string;
  displayTimezone: string;
  locationType: EventLocationTypeValue;
  location: string | null;
  capacity: number | null;
  registeredCount: number;
  status: EventStatusName;
  myRegistrationStatus: EventRegistrationStatusName | null;
}

export interface EventDetail {
  id: string;
  title: string;
  description: string;
  startsAtUtc: string;
  endsAtUtc: string;
  displayTimezone: string;
  locationType: EventLocationTypeValue;
  location: string | null;
  meetingUrl: string | null;
  capacity: number | null;
  registeredCount: number;
  waitlistedCount: number;
  status: EventStatusName;
  myRegistrationStatus: EventRegistrationStatusName | null;
}

export interface MyRegistration {
  eventId: string;
  eventTitle: string;
  startsAtUtc: string;
  displayTimezone: string;
  status: EventRegistrationStatusName;
}

export const eventsApi = {
  listEvents: (includePast: boolean, language: string) =>
    apiRequest<EventSummary[]>(`/events?includePast=${includePast}&language=${language}`),

  // The backend returns 204 No Content (not a JSON `null` body) when there's no upcoming
  // registration — ASP.NET Core's default output formatter rewrites `Ok(null)` into 204 — and
  // apiRequest resolves a 204 to `undefined`. TanStack Query forbids a queryFn resolving to
  // `undefined` (it throws "Query data cannot be undefined"), so this must be coalesced to `null`
  // here at the call site.
  getMyUpcoming: (language: string) =>
    apiRequest<MyRegistration | null>(`/events/my-upcoming?language=${language}`).then((result) => result ?? null),

  getEvent: (eventId: string, language: string) => apiRequest<EventDetail>(`/events/${eventId}?language=${language}`),

  register: (eventId: string) => apiRequest<{ status: EventRegistrationStatusName }>(`/events/${eventId}/register`, { method: "POST" }),

  cancelRegistration: (eventId: string) => apiRequest<void>(`/events/${eventId}/cancel-registration`, { method: "POST" }),
};
