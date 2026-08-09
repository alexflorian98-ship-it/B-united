import { apiRequest } from "../../shared/api/apiClient";
export interface RoomSummary { roomId: string; key: string; name: string; programId: string | null; hasAccess: boolean; lastMessagePreview: string | null; lastMessageAtUtc: string | null; unreadCount: number }
export interface ChatMessage { id: string; userId: string; email: string | null; body: string | null; isPinned: boolean; isDeleted: boolean; createdAt: string }
export interface MessagePage { items: ChatMessage[]; nextBeforeCursor: string | null }
export const chatApi = {
  listRooms: () => apiRequest<RoomSummary[]>("/chat/rooms"),
  getMessages: (roomId: string, before?: string) => apiRequest<MessagePage>(`/chat/rooms/${roomId}/messages${before ? `?before=${encodeURIComponent(before)}` : ""}`),
  sendMessage: (roomId: string, body: string) => apiRequest<ChatMessage>(`/chat/rooms/${roomId}/messages`, { method: "POST", body: { body } }),
  markRead: (roomId: string) => apiRequest<void>(`/chat/rooms/${roomId}/read`, { method: "POST" }),
  reportMessage: (messageId: string, reason: string) => apiRequest<void>(`/chat/messages/${messageId}/report`, { method: "POST", body: { reason } }),
};
