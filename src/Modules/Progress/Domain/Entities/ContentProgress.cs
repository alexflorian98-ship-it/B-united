using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Progress.Domain.Entities;

/// <summary>
/// docs/PROMPT.md §23–24. <see cref="ContentItemId"/> is an opaque reference to Content's
/// `ContentItem` — this module never references Content's Domain/Infrastructure layer
/// (CLAUDE.md), so it has no navigation property and no FK constraint to it.
/// <see cref="IAuditableEntity.UpdatedAt"/> is stamped by the shared save-changes interceptor;
/// <see cref="StartedAt"/>/<see cref="CompletedAt"/> are business timestamps this entity sets
/// itself and are not the same thing.
/// </summary>
public sealed class ContentProgress : IAuditableEntity
{
    public const double CompletionThresholdPercentage = 90.0;

    private ContentProgress()
    {
    }

    public static ContentProgress Start(Guid userId, Guid contentItemId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContentItemId = contentItemId,
            Status = ContentProgressStatus.NotStarted,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ContentItemId { get; private set; }

    public ContentProgressStatus Status { get; private set; }

    public int? LastVideoPositionSeconds { get; private set; }

    public double? WatchPercentage { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Video progress-reporting: periodic (~15s) position persistence plus
    /// pause/navigate/close/complete triggers, auto-completing at the §23–24 threshold. Once
    /// completed, a later (lower) position report never moves it back out of Completed.</summary>
    public void RecordVideoPosition(int positionSeconds, double watchPercentage, DateTime utcNow)
    {
        if (Status == ContentProgressStatus.NotStarted)
        {
            Status = ContentProgressStatus.InProgress;
            StartedAt = utcNow;
        }

        LastVideoPositionSeconds = positionSeconds;
        WatchPercentage = watchPercentage;

        if (Status != ContentProgressStatus.Completed && watchPercentage >= CompletionThresholdPercentage)
        {
            Status = ContentProgressStatus.Completed;
            CompletedAt = utcNow;
        }
    }

    /// <summary>Rich text's explicit "Mark as completed" action — never triggered
    /// automatically.</summary>
    public void MarkCompletedManually(DateTime utcNow)
    {
        StartedAt ??= utcNow;
        Status = ContentProgressStatus.Completed;
        CompletedAt = utcNow;
    }
}
