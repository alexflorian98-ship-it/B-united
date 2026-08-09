using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Application.UseCases.Client;
using BUnited.Modules.Chat.Application.UseCases.Moderation;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Tests.TestSupport;
using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Chat.Tests.Application;

public sealed class ChatFlowTests
{
    private sealed record Fixture(
        TestDbContext DbContext,
        FakeAuditLogger AuditLogger,
        FakeUserLookup UserLookup,
        FakeProgramAccessContext ProgramAccessContext,
        SendMessageHandler SendMessageHandler,
        GetMessagesHandler GetMessagesHandler,
        ReportMessageHandler ReportMessageHandler,
        PinMessageHandler PinMessageHandler,
        DeleteMessageHandler DeleteMessageHandler,
        MuteUserHandler MuteUserHandler,
        ResolveReportHandler ResolveReportHandler,
        ListReportsHandler ListReportsHandler,
        ListMutedUsersHandler ListMutedUsersHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var auditLogger = new FakeAuditLogger();
        var userLookup = new FakeUserLookup();
        var programAccessContext = new FakeProgramAccessContext();
        var deleteMessageHandler = new DeleteMessageHandler(context, auditLogger);
        var muteUserHandler = new MuteUserHandler(context, auditLogger);

        return new Fixture(
            context,
            auditLogger,
            userLookup,
            programAccessContext,
            new SendMessageHandler(context, programAccessContext),
            new GetMessagesHandler(context, userLookup, programAccessContext),
            new ReportMessageHandler(context, programAccessContext),
            new PinMessageHandler(context),
            deleteMessageHandler,
            muteUserHandler,
            new ResolveReportHandler(context, deleteMessageHandler, muteUserHandler),
            new ListReportsHandler(context, userLookup),
            new ListMutedUsersHandler(context, userLookup));
    }

    /// <summary>Seeds an active, program-scoped room and grants the given users access to that
    /// program via the fixture's <see cref="FakeProgramAccessContext"/> — the default shape most
    /// tests need. Tests that specifically exercise the entitlement gate grant access separately
    /// (or not at all).</summary>
    private static ChatRoom SeedAccessibleRoom(Fixture fx, params Guid[] usersWithAccess)
    {
        var programId = Guid.NewGuid();
        var room = ChatRoom.Create(programId, $"room-{Guid.NewGuid():N}", "Test Room", null);
        fx.DbContext.ChatRooms.Add(room);
        fx.DbContext.SaveChanges();

        foreach (var userId in usersWithAccess)
        {
            fx.ProgramAccessContext.GrantAccess(userId, programId);
        }

        return room;
    }

    [Fact]
    public async Task Sending_a_message_persists_it_in_the_room()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, userId);

        var result = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, "Hello everyone"), CancellationToken.None);

        Assert.Equal("Hello everyone", result.Body);
        var page = await fx.GetMessagesHandler.HandleAsync(room.Id, userId, null, CancellationToken.None);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Posting_without_access_to_the_rooms_program_is_denied()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx); // no access granted

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, "Sneaking in"), CancellationToken.None));

        Assert.Equal("PROGRAM_ACCESS_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Reading_history_without_access_to_the_rooms_program_is_denied()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var authorId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, authorId);
        await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, authorId, "Members only"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.GetMessagesHandler.HandleAsync(room.Id, outsiderId, null, CancellationToken.None));

        Assert.Equal("PROGRAM_ACCESS_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Reading_and_posting_in_a_second_program_room_is_denied_by_a_first_program_owner()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var roomA = SeedAccessibleRoom(fx, userId);
        var roomB = SeedAccessibleRoom(fx); // userId has no access to roomB's program

        await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(roomA.Id, userId, "Fine here"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.SendMessageHandler.HandleAsync(new SendMessageCommand(roomB.Id, userId, "Not here"), CancellationToken.None));
        Assert.Equal("PROGRAM_ACCESS_REQUIRED", ex.Code);

        var readEx = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.GetMessagesHandler.HandleAsync(roomB.Id, userId, null, CancellationToken.None));
        Assert.Equal("PROGRAM_ACCESS_REQUIRED", readEx.Code);
    }

    [Fact]
    public async Task A_muted_user_cannot_send_a_message()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, userId);

        await fx.MuteUserHandler.HandleAsync(new MuteUserCommand(userId, moderatorId, 60, "Spam"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, "Sneaking one in"), CancellationToken.None));

        Assert.Equal("CHAT_USER_MUTED", ex.Code);
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "chat.user_muted");
    }

    [Fact]
    public async Task An_expired_mute_no_longer_blocks_sending()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, userId);
        var mute = Mute.Create(userId, Guid.NewGuid(), null, DateTime.UtcNow.AddMinutes(-1));
        fx.DbContext.Mutes.Add(mute);
        await fx.DbContext.SaveChangesAsync();

        var result = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, "Back now"), CancellationToken.None);

        Assert.Equal("Back now", result.Body);
    }

    [Fact]
    public async Task Deleting_a_message_soft_deletes_it_and_hides_its_body_from_the_room_feed()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, userId);
        var sent = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, "Something inappropriate"), CancellationToken.None);

        // Moderation bypasses entitlement entirely — no access grant needed for the moderator.
        await fx.DeleteMessageHandler.HandleAsync(sent.Id, moderatorId, CancellationToken.None);

        var page = await fx.GetMessagesHandler.HandleAsync(room.Id, userId, null, CancellationToken.None);
        var stored = fx.DbContext.Messages.Single(m => m.Id == sent.Id);
        Assert.True(stored.IsDeleted);
        Assert.Equal("Something inappropriate", stored.Body); // history preserved, not erased
        Assert.Null(page.Items.Single().Body); // but not exposed to ordinary room members
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "chat.message_moderated");
    }

    [Fact]
    public async Task Pinning_and_unpinning_a_message_toggles_its_flag()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, userId);
        var sent = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, "Pin me"), CancellationToken.None);

        await fx.PinMessageHandler.SetPinnedAsync(sent.Id, true, CancellationToken.None);
        Assert.True(fx.DbContext.Messages.Single(m => m.Id == sent.Id).IsPinned);

        await fx.PinMessageHandler.SetPinnedAsync(sent.Id, false, CancellationToken.None);
        Assert.False(fx.DbContext.Messages.Single(m => m.Id == sent.Id).IsPinned);
    }

    [Fact]
    public async Task Report_flow_appears_in_the_queue_and_can_be_dismissed()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var authorId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, authorId, reporterId);
        fx.UserLookup.Users[reporterId] = new UserSummary(reporterId, "reporter@example.com", null);
        var sent = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, authorId, "Reported text"), CancellationToken.None);

        await fx.ReportMessageHandler.HandleAsync(new ReportMessageCommand(sent.Id, reporterId, "Off-topic"), CancellationToken.None);

        var openReports = await fx.ListReportsHandler.HandleAsync(ReportStatus.Open, CancellationToken.None);
        Assert.Single(openReports);
        var report = openReports[0];
        Assert.Equal("reporter@example.com", report.ReporterEmail);

        await fx.ResolveReportHandler.HandleAsync(
            new ResolveReportCommand(report.ReportId, moderatorId, ReportResolutionAction.Dismiss, 60, null),
            CancellationToken.None);

        Assert.Empty(await fx.ListReportsHandler.HandleAsync(ReportStatus.Open, CancellationToken.None));
        var dismissed = await fx.ListReportsHandler.HandleAsync(ReportStatus.Dismissed, CancellationToken.None);
        Assert.Single(dismissed);
        // Dismissing must not have deleted the message or muted anyone.
        Assert.False(fx.DbContext.Messages.Single(m => m.Id == sent.Id).IsDeleted);
    }

    [Fact]
    public async Task Reporting_a_message_without_access_to_its_rooms_program_is_denied()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var authorId = Guid.NewGuid();
        var outsiderReporterId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, authorId);
        var sent = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, authorId, "Reported text"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.ReportMessageHandler.HandleAsync(new ReportMessageCommand(sent.Id, outsiderReporterId, "Off-topic"), CancellationToken.None));

        Assert.Equal("PROGRAM_ACCESS_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Resolving_a_report_with_MuteUser_mutes_the_message_author()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var authorId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, authorId, reporterId);
        var sent = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, authorId, "Reported text"), CancellationToken.None);
        await fx.ReportMessageHandler.HandleAsync(new ReportMessageCommand(sent.Id, reporterId, "Abuse"), CancellationToken.None);
        var report = (await fx.ListReportsHandler.HandleAsync(ReportStatus.Open, CancellationToken.None))[0];

        // Moderation bypasses entitlement entirely — no access grant needed for the moderator.
        await fx.ResolveReportHandler.HandleAsync(
            new ResolveReportCommand(report.ReportId, moderatorId, ReportResolutionAction.MuteUser, 30, "Abuse"),
            CancellationToken.None);

        var muted = await fx.ListMutedUsersHandler.HandleAsync(CancellationToken.None);
        Assert.Contains(muted, m => m.UserId == authorId);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, authorId, "Trying again"), CancellationToken.None));
        Assert.Equal("CHAT_USER_MUTED", ex.Code);
    }

    [Fact]
    public async Task Resolving_a_report_with_DeleteMessage_soft_deletes_the_reported_message()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var authorId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, authorId, reporterId);
        var sent = await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, authorId, "Reported text"), CancellationToken.None);
        await fx.ReportMessageHandler.HandleAsync(new ReportMessageCommand(sent.Id, reporterId, "Abuse"), CancellationToken.None);
        var report = (await fx.ListReportsHandler.HandleAsync(ReportStatus.Open, CancellationToken.None))[0];

        await fx.ResolveReportHandler.HandleAsync(
            new ResolveReportCommand(report.ReportId, moderatorId, ReportResolutionAction.DeleteMessage, 30, null),
            CancellationToken.None);

        Assert.True(fx.DbContext.Messages.Single(m => m.Id == sent.Id).IsDeleted);
    }

    [Fact]
    public async Task Pagination_returns_a_cursor_only_when_a_full_page_was_returned()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var room = SeedAccessibleRoom(fx, userId);
        for (var i = 0; i < 5; i++)
        {
            await fx.SendMessageHandler.HandleAsync(new SendMessageCommand(room.Id, userId, $"msg {i}"), CancellationToken.None);
        }

        var page = await fx.GetMessagesHandler.HandleAsync(room.Id, userId, null, CancellationToken.None);

        Assert.Equal(5, page.Items.Count);
        Assert.Null(page.NextBeforeCursor);
        // Ascending (oldest-first) order for display.
        Assert.Equal("msg 0", page.Items[0].Body);
        Assert.Equal("msg 4", page.Items[^1].Body);
    }
}
