import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { EventsListPage } from "./EventsListPage";
import { eventsApi, type EventSummary } from "./eventsApi";

vi.mock("./eventsApi");

function renderEventsListPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <EventsListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const openEvent: EventSummary = {
  id: "event-1",
  title: "Live Q&A",
  startsAtUtc: "2026-09-01T18:00:00Z",
  endsAtUtc: "2026-09-01T19:00:00Z",
  displayTimezone: "Europe/Bucharest",
  locationType: 0,
  location: null,
  capacity: 10,
  registeredCount: 3,
  status: "Published",
  myRegistrationStatus: "Registered",
};

describe("EventsListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("lists upcoming events with title and registration status", async () => {
    vi.mocked(eventsApi.listEvents).mockResolvedValue([openEvent]);
    renderEventsListPage();

    expect(await screen.findByText("Live Q&A")).toBeInTheDocument();
    expect(screen.getByText("Registered")).toBeInTheDocument();
    expect(eventsApi.listEvents).toHaveBeenCalledWith(false, expect.any(String));
  });

  it("shows an empty state when there are no events", async () => {
    vi.mocked(eventsApi.listEvents).mockResolvedValue([]);
    renderEventsListPage();

    expect(await screen.findByText("No events yet")).toBeInTheDocument();
  });

  it("shows a generic error alert when the request fails", async () => {
    vi.mocked(eventsApi.listEvents).mockRejectedValue(new Error("network error"));
    renderEventsListPage();

    expect(await screen.findByText("Something went wrong. Please try again.")).toBeInTheDocument();
  });

  it("switches to the past tab and re-queries with includePast=true", async () => {
    vi.mocked(eventsApi.listEvents).mockResolvedValue([openEvent]);
    const user = userEvent.setup();
    renderEventsListPage();

    await screen.findByText("Live Q&A");
    await user.click(screen.getByRole("button", { name: "Past" }));

    await waitFor(() => expect(eventsApi.listEvents).toHaveBeenCalledWith(true, expect.any(String)));
  });
});
