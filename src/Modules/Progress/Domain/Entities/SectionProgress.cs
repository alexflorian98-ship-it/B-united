using BUnited.BuildingBlocks.Domain;
using BUnited.Modules.Progress.Domain;

namespace BUnited.Modules.Progress.Domain.Entities;

/// <summary>
/// Denormalized per (user, section) completion cache for efficient dashboard/player rendering
/// (docs/PROMPT.md §23–24) — recalculated whenever a <see cref="ContentProgress"/> row in the
/// section changes. <see cref="SectionId"/> is opaque (see <see cref="ContentProgress"/>'s own
/// remarks); <see cref="TotalItemCount"/> is supplied by the caller (the player already has the
/// section's structure loaded from the Content API) rather than looked up here, so this module
/// never needs to reference Content's Domain layer at all.
/// </summary>
public sealed class SectionProgress : IAuditableEntity
{
    private SectionProgress()
    {
    }

    public static SectionProgress Create(Guid userId, Guid sectionId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SectionId = sectionId,
            Status = ContentProgressStatus.NotStarted,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid SectionId { get; private set; }

    public ContentProgressStatus Status { get; private set; }

    public int CompletedItemCount { get; private set; }

    public int TotalItemCount { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Deterministic: the same (completedItemCount, totalItemCount) always yields the
    /// same status, regardless of call order (P2.28.b).</summary>
    public void Recalculate(int completedItemCount, int totalItemCount)
    {
        CompletedItemCount = completedItemCount;
        TotalItemCount = totalItemCount;

        Status = totalItemCount > 0 && completedItemCount >= totalItemCount
            ? ContentProgressStatus.Completed
            : completedItemCount > 0
                ? ContentProgressStatus.InProgress
                : ContentProgressStatus.NotStarted;
    }
}
