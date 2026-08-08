using BUnited.Modules.Progress.Application.UseCases;
using BUnited.Modules.Progress.Tests.TestSupport;

namespace BUnited.Modules.Progress.Tests.Application;

public sealed class ProgressFlowTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Recording_video_progress_updates_both_content_and_section_progress()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var videoItemId = Guid.NewGuid();
        var richTextItemId = Guid.NewGuid();
        var sectionItems = new[] { videoItemId, richTextItemId };

        var recordHandler = new RecordVideoProgressHandler(context, TimeProvider.System);
        await recordHandler.HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 540, 95.0),
            CancellationToken.None);

        var contentProgress = await new GetContentProgressHandler(context).HandleAsync(UserId, [videoItemId], CancellationToken.None);
        Assert.Equal("Completed", Assert.Single(contentProgress).Status);

        var sectionProgress = await new GetSectionProgressHandler(context).HandleAsync(UserId, [sectionId], CancellationToken.None);
        var section = Assert.Single(sectionProgress);
        Assert.Equal(1, section.CompletedItemCount);
        Assert.Equal(2, section.TotalItemCount);
        Assert.Equal("InProgress", section.Status);
    }

    [Fact]
    public async Task Marking_the_last_item_completed_marks_the_whole_section_completed()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var videoItemId = Guid.NewGuid();
        var richTextItemId = Guid.NewGuid();
        var sectionItems = new[] { videoItemId, richTextItemId };

        await new RecordVideoProgressHandler(context, TimeProvider.System).HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 540, 95.0), CancellationToken.None);

        await new MarkContentCompletedHandler(context, TimeProvider.System).HandleAsync(
            new MarkContentCompletedCommand(UserId, richTextItemId, sectionId, sectionItems), CancellationToken.None);

        var sectionProgress = await new GetSectionProgressHandler(context).HandleAsync(UserId, [sectionId], CancellationToken.None);
        var section = Assert.Single(sectionProgress);
        Assert.Equal(2, section.CompletedItemCount);
        Assert.Equal("Completed", section.Status);
    }

    [Fact]
    public async Task Progress_for_one_user_is_isolated_from_another_users_progress_on_the_same_item()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var otherUserId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var sectionItems = new[] { itemId };

        await new MarkContentCompletedHandler(context, TimeProvider.System).HandleAsync(
            new MarkContentCompletedCommand(UserId, itemId, sectionId, sectionItems), CancellationToken.None);

        var otherUsersProgress = await new GetContentProgressHandler(context).HandleAsync(otherUserId, [itemId], CancellationToken.None);
        Assert.Empty(otherUsersProgress);

        var thisUsersProgress = await new GetContentProgressHandler(context).HandleAsync(UserId, [itemId], CancellationToken.None);
        Assert.Equal("Completed", Assert.Single(thisUsersProgress).Status);
    }
}
