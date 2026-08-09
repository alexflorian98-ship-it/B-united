using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Chat.Domain.Entities;

/// <summary>docs/PROMPT.md §33-34. Soft delete only (`IsDeleted`) — deleted messages are never
/// physically removed, so room history stays coherent (§66's anonymization requirement operates
/// on the author's identity, not the message row).</summary>
public sealed class Message : IAuditableEntity
{
    private Message()
    {
    }

    public static Message Create(Guid roomId, Guid userId, string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            Body = body,
            IsPinned = false,
            IsDeleted = false,
        };

    public Guid Id { get; private set; }

    /// <summary>Same-module FK to <see cref="ChatRoom.Id"/> — replaces the old fixed <c>ChatRoom</c>
    /// enum column (docs/TASKS.md P3.43.a).</summary>
    public Guid RoomId { get; private set; }

    public Guid UserId { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public bool IsPinned { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Pin() => IsPinned = true;

    public void Unpin() => IsPinned = false;

    public void SoftDelete(Guid moderatorId, DateTime utcNow)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedBy = moderatorId;
        IsPinned = false;
    }
}
