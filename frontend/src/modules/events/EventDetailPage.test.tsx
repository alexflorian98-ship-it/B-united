import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { EventDetailPage } from "./EventDetailPage";
import { eventsApi, type EventDetail } from "./eventsApi";

vi.mock("./eventsApi");

function renderEventDetailPage(eventId = "event-1") {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/events/${eventId}`]}>
        <Routes>
          <Route path="/events/:eventId" element={<EventDetailPage />} />
          <Route path="/events" element={<div>Events list page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const openEvent: EventDetail = {
  id: "event-1",
  title: "Live Q&A",
  description: "A live session with the expert.",
  startsAtUtc: "2026-09-01T18:00:00Z",
  endsAtUtc: "2026-09-01T19:00:00Z",
  displayTimezone: "Europe/Bucharest",
  locationType: 0,
  location: null,
  meetingUrl: null,
  capacity: 10,
  registeredCount: 3,
  waitlistedCount: 0,
  status: "Published",
  myRegistrationStatus: null,
};

describe("EventDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a not-found alert when the event fails to load", async () => {
    vi.mocked(eventsApi.getEvent).mockRejectedValue(new Error("not found"));
    renderEventDetailPage();

    expect(await screen.findByText("We couldn't find what you were looking for.")).toBeInTheDocument();
  });

  it("registers for an event and shows success feedback", async () => {
    vi.mocked(eventsApi.getEvent).mockResolvedValue(openEvent);
    vi.mocked(eventsApi.register).mockResolvedValue({ status: "Registered" });
    const user = userEvent.setup();
    renderEventDetailPage();

    await screen.findByText("Live Q&A");
    await user.click(screen.getByRole("button", { name: "Register" }));

    expect(await screen.findByText("You're registered for this event.")).toBeInTheDocument();
    expect(eventsApi.register).toHaveBeenCalledWith("event-1");
  });

  it("shows waitlisted feedback when the event is full", async () => {
    vi.mocked(eventsApi.getEvent).mockResolvedValue(openEvent);
    vi.mocked(eventsApi.register).mockResolvedValue({ status: "Waitlisted" });
    const user = userEvent.setup();
    renderEventDetailPage();

    await screen.findByText("Live Q&A");
    await user.click(screen.getByRole("button", { name: "Register" }));

    expect(await screen.findByText("This event is full — you've been added to the waitlist.")).toBeInTheDocument();
  });

  it("shows a Cancel button and cancels an existing registration", async () => {
    vi.mocked(eventsApi.getEvent).mockResolvedValue({ ...openEvent, myRegistrationStatus: "Registered" });
    vi.mocked(eventsApi.cancelRegistration).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderEventDetailPage();

    await screen.findByText("Live Q&A");
    expect(screen.queryByRole("button", { name: "Register" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancel registration" }));

    await waitFor(() => expect(eventsApi.cancelRegistration).toHaveBeenCalledWith("event-1"));
  });

  it("navigates back to the events list", async () => {
    vi.mocked(eventsApi.getEvent).mockResolvedValue(openEvent);
    const user = userEvent.setup();
    renderEventDetailPage();

    await screen.findByText("Live Q&A");
    await user.click(screen.getByRole("button", { name: "Back" }));

    expect(await screen.findByText("Events list page")).toBeInTheDocument();
  });
});
