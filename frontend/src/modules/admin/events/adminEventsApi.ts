import { apiRequest } from "../../../shared/api/apiClient";
import type { EventLocationTypeValue } from "../../events/eventsApi";

export type { EventLocationTypeValue } from "../../events/eventsApi";

export interface AdminEventListItem {
  id: string;
  title: string;
  startsAtUtc: string;
  displayTimezone: string;
  locationType: EventLocationTypeValue;
  registeredCount: number;
  capacity: number | null;
  status: string;
}

export interface AdminEventListResult {
  items: AdminEventListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EventTranslationEntry {
  language: string;
  title: string;
  description: string;
}

export interface EventRegistrationSummary {
  registrationId: string;
  userId: string;
  email: string | null;
  status: string;
  registeredAt: string;
}

export interface EventReminderSummary {
  registrationId: string;
  email: string | null;
  type: string;
  scheduledForUtc: string;
  sentAtUtc: string | null;
}

export interface AdminEventDetail {
  id: string;
  startsAtUtc: string;
  endsAtUtc: string;
  displayTimezone: string;
  locationType: EventLocationTypeValue;
  location: string | null;
  meetingUrl: string | null;
  capacity: number | null;
  status: string;
  defaultLanguage: string;
  translations: EventTranslationEntry[];
  registrations: EventRegistrationSummary[];
  waitlist: EventRegistrationSummary[];
  reminders: EventReminderSummary[];
  programIds: string[];
}

export interface CreateEventInput {
  defaultLanguage: string;
  title: string;
  description: string;
  startsAtUtc: string;
  endsAtUtc: string;
  displayTimezone: string;
  locationType: EventLocationTypeValue;
  location: string | null;
  meetingUrl: string | null;
  capacity: number | null;
}

export interface UpdateEventScheduleInput {
  startsAtUtc: string;
  endsAtUtc: string;
  displayTimezone: string;
  locationType: EventLocationTypeValue;
  location: string | null;
  meetingUrl: string | null;
  capacity: number | null;
}

export const adminEventsApi = {
  listEvents: (page = 1, pageSize = 25) => apiRequest<AdminEventListResult>(`/admin/events?page=${page}&pageSize=${pageSize}`),

  getEvent: (eventId: string) => apiRequest<AdminEventDetail>(`/admin/events/${eventId}`),

  createEvent: (input: CreateEventInput) => apiRequest<string>("/admin/events", { method: "POST", body: input }),

  upsertTranslation: (eventId: string, language: string, title: string, description: string) =>
    apiRequest<void>(`/admin/events/${eventId}/translations`, { method: "PUT", body: { language, title, description } }),

  updateSchedule: (eventId: string, input: UpdateEventScheduleInput) =>
    apiRequest<void>(`/admin/events/${eventId}/schedule`, { method: "PUT", body: input }),

  setPrograms: (eventId: string, programIds: string[]) =>
    apiRequest<void>(`/admin/events/${eventId}/programs`, { method: "PUT", body: { programIds } }),

  publish: (eventId: string) => apiRequest<void>(`/admin/events/${eventId}/publish`, { method: "POST" }),

  cancel: (eventId: string) => apiRequest<void>(`/admin/events/${eventId}/cancel`, { method: "POST" }),
};
