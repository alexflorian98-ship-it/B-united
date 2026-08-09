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

  getMyUpcoming: (language: string) => apiRequest<MyRegistration | null>(`/events/my-upcoming?language=${language}`),

  getEvent: (eventId: string, language: string) => apiRequest<EventDetail>(`/events/${eventId}?language=${language}`),

  register: (eventId: string) => apiRequest<{ status: EventRegistrationStatusName }>(`/events/${eventId}/register`, { method: "POST" }),

  cancelRegistration: (eventId: string) => apiRequest<void>(`/events/${eventId}/cancel-registration`, { method: "POST" }),
};
