namespace BUnited.Modules.Chat.Domain.Entities;

/// <summary>P6.06 — basic unread-state tracking: one row per (UserId, RoomId), holding the
/// timestamp of the last message the user is known to have seen. Unread state is a simple
/// derived comparison (`latestMessage.CreatedAt > LastReadAtUtc`), not a per-message read
/// receipt table.</summary>
public sealed class ChatReadState
{
    private ChatReadState()
    {
    }

    public static ChatReadState Create(Guid userId, Guid roomId, DateTime lastReadAtUtc) =>
        new()
        {
            UserId = userId,
            RoomId = roomId,
            LastReadAtUtc = lastReadAtUtc,
        };

    public Guid UserId { get; private set; }

    /// <summary>Same-module FK to <see cref="ChatRoom.Id"/> — replaces the old fixed <c>ChatRoom</c>
    /// enum column (docs/TASKS.md P3.43.a).</summary>
    public Guid RoomId { get; private set; }

    public DateTime LastReadAtUtc { get; private set; }

    public void MarkRead(DateTime utcNow) => LastReadAtUtc = utcNow;
}
