using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Progress.Application.UseCases;
using BUnited.Modules.Progress.Tests.TestSupport;

namespace BUnited.Modules.Progress.Tests.Application;

public sealed class ProgressFlowTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Wires a section/content item pair to a fresh program and grants the given users
    /// access, so each test only has to declare what it needs.</summary>
    private static (Guid ProgramId, FakeContentItemProgramLookup Lookup, FakeProgramAccessContext Access) SetUpOwnedProgram(
        Guid sectionId, IReadOnlyCollection<Guid> contentItemIds, params Guid[] ownerUserIds)
    {
        var programId = Guid.NewGuid();
        var lookup = new FakeContentItemProgramLookup();
        var access = new FakeProgramAccessContext();

        lookup.MapSection(sectionId, programId);
        foreach (var contentItemId in contentItemIds)
        {
            lookup.MapContentItem(contentItemId, programId);
        }

        foreach (var ownerUserId in ownerUserIds)
        {
            access.GrantAccess(ownerUserId, programId);
        }

        return (programId, lookup, access);
    }

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
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems, UserId);

        var recordHandler = new RecordVideoProgressHandler(context, TimeProvider.System, lookup, access);
        await recordHandler.HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 540, 95.0),
            CancellationToken.None);

        var contentProgress = await new GetContentProgressHandler(context, lookup, access).HandleAsync(UserId, [videoItemId], CancellationToken.None);
        Assert.Equal("Completed", Assert.Single(contentProgress).Status);

        var sectionProgress = await new GetSectionProgressHandler(context, lookup, access).HandleAsync(UserId, [sectionId], CancellationToken.None);
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
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems, UserId);

        await new RecordVideoProgressHandler(context, TimeProvider.System, lookup, access).HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 540, 95.0), CancellationToken.None);

        await new MarkContentCompletedHandler(context, TimeProvider.System, lookup, access).HandleAsync(
            new MarkContentCompletedCommand(UserId, richTextItemId, sectionId, sectionItems), CancellationToken.None);

        var sectionProgress = await new GetSectionProgressHandler(context, lookup, access).HandleAsync(UserId, [sectionId], CancellationToken.None);
        var section = Assert.Single(sectionProgress);
        Assert.Equal(2, section.CompletedItemCount);
        Assert.Equal("Completed", section.Status);
    }

    [Fact]
    public async Task Recording_video_progress_round_trips_the_exact_resume_position_and_watch_percentage()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var videoItemId = Guid.NewGuid();
        var sectionItems = new[] { videoItemId };
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems, UserId);

        var recordHandler = new RecordVideoProgressHandler(context, TimeProvider.System, lookup, access);
        await recordHandler.HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 213, 42.5),
            CancellationToken.None);

        var contentProgress = await new GetContentProgressHandler(context, lookup, access).HandleAsync(UserId, [videoItemId], CancellationToken.None);
        var dto = Assert.Single(contentProgress);
        Assert.Equal(213, dto.LastVideoPositionSeconds);
        Assert.Equal(42.5, dto.WatchPercentage);
        Assert.Equal("InProgress", dto.Status);

        // A later report (e.g. the user resumed and skipped further ahead) must overwrite,
        // not accumulate, the persisted position.
        await recordHandler.HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 480, 76.0),
            CancellationToken.None);

        var updatedProgress = await new GetContentProgressHandler(context, lookup, access).HandleAsync(UserId, [videoItemId], CancellationToken.None);
        var updatedDto = Assert.Single(updatedProgress);
        Assert.Equal(480, updatedDto.LastVideoPositionSeconds);
        Assert.Equal(76.0, updatedDto.WatchPercentage);
        Assert.Equal("InProgress", updatedDto.Status);
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
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems, UserId, otherUserId);

        await new MarkContentCompletedHandler(context, TimeProvider.System, lookup, access).HandleAsync(
            new MarkContentCompletedCommand(UserId, itemId, sectionId, sectionItems), CancellationToken.None);

        var otherUsersProgress = await new GetContentProgressHandler(context, lookup, access).HandleAsync(otherUserId, [itemId], CancellationToken.None);
        Assert.Empty(otherUsersProgress);

        var thisUsersProgress = await new GetContentProgressHandler(context, lookup, access).HandleAsync(UserId, [itemId], CancellationToken.None);
        Assert.Equal("Completed", Assert.Single(thisUsersProgress).Status);
    }

    [Fact]
    public async Task Recording_video_progress_is_denied_when_the_user_does_not_own_the_owning_program()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var videoItemId = Guid.NewGuid();
        var sectionItems = new[] { videoItemId };
        // No GrantAccess call: the program exists but UserId has not purchased it.
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems);

        var handler = new RecordVideoProgressHandler(context, TimeProvider.System, lookup, access);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(() => handler.HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 540, 95.0), CancellationToken.None));
        Assert.Equal("PROGRAM_ACCESS_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Marking_content_completed_is_denied_when_the_user_does_not_own_the_owning_program()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var sectionItems = new[] { itemId };
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems);

        var handler = new MarkContentCompletedHandler(context, TimeProvider.System, lookup, access);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(() => handler.HandleAsync(
            new MarkContentCompletedCommand(UserId, itemId, sectionId, sectionItems), CancellationToken.None));
        Assert.Equal("PROGRAM_ACCESS_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Getting_content_progress_is_denied_when_the_user_does_not_own_the_owning_program()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var sectionItems = new[] { itemId };
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems);

        var handler = new GetContentProgressHandler(context, lookup, access);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            handler.HandleAsync(UserId, [itemId], CancellationToken.None));
        Assert.Equal("PROGRAM_ACCESS_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Getting_section_progress_is_denied_when_the_user_does_not_own_the_owning_program()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var sectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var sectionItems = new[] { itemId };
        var (_, lookup, access) = SetUpOwnedProgram(sectionId, sectionItems);

        var handler = new GetSectionProgressHandler(context, lookup, access);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            handler.HandleAsync(UserId, [sectionId], CancellationToken.None));
        Assert.Equal("PROGRAM_ACCESS_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Concurrent_progress_reports_for_the_same_section_do_not_throw_or_duplicate_the_section_row()
    {
        var (connection1, context1, connection2, context2) = TestDbContextFactory.CreateConcurrentPair();
        using var _1 = connection1;
        using var _2 = connection2;
        using var __1 = context1;
        using var __2 = context2;

        var sectionId = Guid.NewGuid();
        var videoItemId = Guid.NewGuid();
        var richTextItemId = Guid.NewGuid();
        var sectionItems = new[] { videoItemId, richTextItemId };

        // Both handlers need their own lookup/access fakes wired against the SAME program/section,
        // since FakeContentItemProgramLookup/FakeProgramAccessContext hold in-memory state that
        // isn't shared across the two DbContext instances any more than the real cross-module
        // contracts would be.
        var (_, lookup1, access1) = SetUpOwnedProgram(sectionId, sectionItems, UserId);
        var (_, lookup2, access2) = SetUpOwnedProgram(sectionId, sectionItems, UserId);

        var handler1 = new RecordVideoProgressHandler(context1, TimeProvider.System, lookup1, access1);
        var handler2 = new MarkContentCompletedHandler(context2, TimeProvider.System, lookup2, access2);

        // Two different content items in the same section, reported at the same time — both race
        // to be the first to create that section's SectionProgress row (P5.06.c-style real
        // concurrent-writer race, found live via Playwright: two video-position reports for the
        // same section, e.g. from a React StrictMode double-invoked mount effect, threw an
        // unhandled DbUpdateException on the ix_section_progress_entries_user_id_section_id
        // unique index before this handler learned to recover from it).
        var task1 = Task.Run(() => handler1.HandleAsync(
            new RecordVideoProgressCommand(UserId, videoItemId, sectionId, sectionItems, 540, 95.0), CancellationToken.None));
        var task2 = Task.Run(() => handler2.HandleAsync(
            new MarkContentCompletedCommand(UserId, richTextItemId, sectionId, sectionItems), CancellationToken.None));
        await Task.WhenAll(task1, task2);

        var sectionProgress = await new GetSectionProgressHandler(context1, lookup1, access1).HandleAsync(UserId, [sectionId], CancellationToken.None);
        var section = Assert.Single(sectionProgress);
        Assert.Equal(2, section.CompletedItemCount);
        Assert.Equal(2, section.TotalItemCount);
        Assert.Equal("Completed", section.Status);
    }

    [Fact]
    public async Task Recording_video_progress_for_an_unknown_content_item_throws_not_found()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var lookup = new FakeContentItemProgramLookup();
        var access = new FakeProgramAccessContext();
        var handler = new RecordVideoProgressHandler(context, TimeProvider.System, lookup, access);

        var unknownItemId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundAppException>(() => handler.HandleAsync(
            new RecordVideoProgressCommand(UserId, unknownItemId, sectionId, [unknownItemId], 10, 5.0), CancellationToken.None));
    }
}
