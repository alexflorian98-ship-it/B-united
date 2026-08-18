import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ChatPage } from "./ChatPage";
import { chatApi, type ChatMessage, type MessagePage, type RoomSummary } from "./chatApi";

vi.mock("./chatApi");

function renderChatPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ChatPage />
    </QueryClientProvider>,
  );
}

const room: RoomSummary = {
  roomId: "room-1",
  key: "general",
  name: "General",
  programId: null,
  hasAccess: true,
  lastMessagePreview: null,
  lastMessageAtUtc: null,
  unreadCount: 0,
};

const newestMessage: ChatMessage = {
  id: "m2",
  userId: "u1",
  email: "second@example.com",
  body: "Second message",
  isPinned: false,
  isDeleted: false,
  createdAt: "2026-01-01T10:01:00Z",
};

const olderMessage: ChatMessage = {
  id: "m1",
  userId: "u1",
  email: "first@example.com",
  body: "First message",
  isPinned: false,
  isDeleted: false,
  createdAt: "2026-01-01T10:00:00Z",
};

const newestPage: MessagePage = { items: [newestMessage], nextBeforeCursor: newestMessage.createdAt };
const olderPage: MessagePage = { items: [olderMessage], nextBeforeCursor: null };

describe("ChatPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(chatApi.listRooms).mockResolvedValue([room]);
    vi.mocked(chatApi.markRead).mockResolvedValue(undefined);
  });

  it("shows a 'Load older messages' button when a cursor is available, and hides it once exhausted", async () => {
    vi.mocked(chatApi.getMessages).mockImplementation((_roomId, before) => Promise.resolve(before ? olderPage : newestPage));
    const user = userEvent.setup();
    renderChatPage();

    await user.click(await screen.findByRole("button", { name: "General" }));

    expect(await screen.findByText("Second message")).toBeInTheDocument();
    expect(screen.queryByText("First message")).not.toBeInTheDocument();
    const loadOlderButton = await screen.findByRole("button", { name: "Load older messages" });

    await user.click(loadOlderButton);

    expect(await screen.findByText("First message")).toBeInTheDocument();
    expect(screen.getByText("Second message")).toBeInTheDocument();
    expect(chatApi.getMessages).toHaveBeenCalledWith("room-1", newestMessage.createdAt);
    await waitFor(() => expect(screen.queryByRole("button", { name: "Load older messages" })).not.toBeInTheDocument());
  });

  it("does not show a 'Load older messages' button when there is no further page", async () => {
    vi.mocked(chatApi.getMessages).mockResolvedValue({ items: [newestMessage], nextBeforeCursor: null });
    const user = userEvent.setup();
    renderChatPage();

    await user.click(await screen.findByRole("button", { name: "General" }));

    expect(await screen.findByText("Second message")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Load older messages" })).not.toBeInTheDocument();
  });

  it("sends a new message and clears the draft", async () => {
    vi.mocked(chatApi.getMessages).mockResolvedValue({ items: [newestMessage], nextBeforeCursor: null });
    vi.mocked(chatApi.sendMessage).mockResolvedValue({ ...newestMessage, id: "m3", body: "Hello room" });
    const user = userEvent.setup();
    renderChatPage();

    await user.click(await screen.findByRole("button", { name: "General" }));
    await screen.findByText("Second message");

    const input = screen.getByPlaceholderText("Write a message…");
    await user.type(input, "Hello room");
    await user.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() => expect(chatApi.sendMessage).toHaveBeenCalledWith("room-1", "Hello room"));
    await waitFor(() => expect(input).toHaveValue(""));
  });
});
