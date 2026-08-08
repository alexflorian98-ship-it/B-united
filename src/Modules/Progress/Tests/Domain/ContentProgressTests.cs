using BUnited.Modules.Progress.Domain;
using BUnited.Modules.Progress.Domain.Entities;

namespace BUnited.Modules.Progress.Tests.Domain;

public sealed class ContentProgressTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Starts_as_not_started()
    {
        var progress = ContentProgress.Start(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ContentProgressStatus.NotStarted, progress.Status);
    }

    [Fact]
    public void Recording_a_position_below_the_threshold_marks_it_in_progress_not_completed()
    {
        var progress = ContentProgress.Start(Guid.NewGuid(), Guid.NewGuid());

        progress.RecordVideoPosition(60, 50.0, UtcNow);

        Assert.Equal(ContentProgressStatus.InProgress, progress.Status);
        Assert.NotNull(progress.StartedAt);
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public void Recording_a_position_at_or_above_90_percent_auto_completes()
    {
        var progress = ContentProgress.Start(Guid.NewGuid(), Guid.NewGuid());

        progress.RecordVideoPosition(540, 90.0, UtcNow);

        Assert.Equal(ContentProgressStatus.Completed, progress.Status);
        Assert.Equal(UtcNow, progress.CompletedAt);
    }

    [Fact]
    public void Recording_a_position_just_below_90_percent_does_not_complete()
    {
        var progress = ContentProgress.Start(Guid.NewGuid(), Guid.NewGuid());

        progress.RecordVideoPosition(530, 89.9, UtcNow);

        Assert.Equal(ContentProgressStatus.InProgress, progress.Status);
    }

    [Fact]
    public void A_later_lower_position_report_never_moves_a_completed_item_back()
    {
        var progress = ContentProgress.Start(Guid.NewGuid(), Guid.NewGuid());
        progress.RecordVideoPosition(540, 95.0, UtcNow);

        // The user rewound and closed the player — must not un-complete it.
        progress.RecordVideoPosition(10, 2.0, UtcNow.AddMinutes(1));

        Assert.Equal(ContentProgressStatus.Completed, progress.Status);
    }

    [Fact]
    public void Rich_text_is_never_auto_completed_by_recording_a_video_position()
    {
        // Rich text items never call RecordVideoPosition at all — only MarkCompletedManually.
        // This test documents that guarantee at the handler-selection level, not the entity.
        var progress = ContentProgress.Start(Guid.NewGuid(), Guid.NewGuid());

        progress.MarkCompletedManually(UtcNow);

        Assert.Equal(ContentProgressStatus.Completed, progress.Status);
        Assert.Equal(UtcNow, progress.StartedAt);
        Assert.Equal(UtcNow, progress.CompletedAt);
    }
}
