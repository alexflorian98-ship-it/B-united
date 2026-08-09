using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Chat.Domain.Entities;

/// <summary>docs/PROMPT.md §33-34 — "temporary mute". No `IsActive` flag: whether a mute is
/// currently in effect is always derived from <see cref="IsActiveAt"/>, the same
/// "no background sweep job" pattern Billing's `Entitlement` and Events' `Event` use — a mute
/// simply stops mattering once its own expiry passes, with nothing needing to update it.</summary>
public sealed class Mute : IAuditableEntity
{
    private Mute()
    {
    }

    public static Mute Create(Guid userId, Guid moderatorId, string? reason, DateTime expiresAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ModeratorId = moderatorId,
            Reason = reason,
            ExpiresAtUtc = expiresAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ModeratorId { get; private set; }

    public string? Reason { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActiveAt(DateTime utcNow) => utcNow < ExpiresAtUtc;
}
