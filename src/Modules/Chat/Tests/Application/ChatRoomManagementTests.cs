using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Application.UseCases.Admin;
using BUnited.Modules.Chat.Application.UseCases.Client;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Tests.TestSupport;
using BUnited.Modules.Content.Contracts;

namespace BUnited.Modules.Chat.Tests.Application;

public sealed class ChatRoomManagementTests
{
    private sealed record Fixture(
        TestDbContext DbContext,
        FakeProgramLookup ProgramLookup,
        FakeProgramAccessContext ProgramAccessContext,
        CreateChatRoomHandler CreateHandler,
        UpdateChatRoomHandler UpdateHandler,
        ListChatRoomsAdminHandler ListAdminHandler,
        ListRoomsHandler ListRoomsHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var programLookup = new FakeProgramLookup();
        var programAccessContext = new FakeProgramAccessContext();

        return new Fixture(
            context,
            programLookup,
            programAccessContext,
            new CreateChatRoomHandler(context, programLookup),
            new UpdateChatRoomHandler(context),
            new ListChatRoomsAdminHandler(context),
            new ListRoomsHandler(context, programAccessContext));
    }

    [Fact]
    public async Task Creating_a_room_for_a_published_program_succeeds()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId, ProgramLookupStatus.Published);

        var roomId = await fx.CreateHandler.HandleAsync(new CreateChatRoomCommand(programId, "meditation", "Meditation", Guid.NewGuid()), CancellationToken.None);

        var adminList = await fx.ListAdminHandler.HandleAsync(CancellationToken.None);
        Assert.Contains(adminList, r => r.Id == roomId && r.ProgramId == programId && r.IsActive);
    }

    [Fact]
    public async Task Creating_a_room_for_a_non_published_program_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId, ProgramLookupStatus.Draft);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.CreateHandler.HandleAsync(new CreateChatRoomCommand(programId, "meditation", "Meditation", Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("CHAT_ROOM_PROGRAM_NOT_PUBLISHED", ex.Code);
    }

    [Fact]
    public async Task Creating_a_room_with_a_duplicate_key_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        await fx.CreateHandler.HandleAsync(new CreateChatRoomCommand(programId, "meditation", "Meditation", Guid.NewGuid()), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.CreateHandler.HandleAsync(new CreateChatRoomCommand(programId, "meditation", "Meditation Two", Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("CHAT_ROOM_KEY_TAKEN", ex.Code);
    }

    [Fact]
    public async Task Deactivating_a_room_removes_it_from_client_discovery()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        var roomId = await fx.CreateHandler.HandleAsync(new CreateChatRoomCommand(programId, "meditation", "Meditation", Guid.NewGuid()), CancellationToken.None);
        var userId = Guid.NewGuid();
        fx.ProgramAccessContext.GrantAccess(userId, programId);

        await fx.UpdateHandler.HandleAsync(new UpdateChatRoomCommand(roomId, "Meditation", false, Guid.NewGuid()), CancellationToken.None);

        var rooms = await fx.ListRoomsHandler.HandleAsync(userId, CancellationToken.None);
        Assert.Empty(rooms);
    }

    [Fact]
    public async Task Legacy_deactivated_rooms_never_appear_in_client_discovery()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        // Simulates the 6 legacy rooms seeded as inactive by the ChatRoom migration
        // (docs/TASKS.md P3.43.a) — the migration's real seed rows use a null ProgramId, but the
        // exact value is irrelevant here: IsActive=false alone already excludes the room from
        // ListRoomsHandler's discovery query.
        fx.DbContext.ChatRooms.Add(LegacyInactiveRoom("general", "General"));
        fx.DbContext.SaveChanges();
        var userId = Guid.NewGuid();

        var rooms = await fx.ListRoomsHandler.HandleAsync(userId, CancellationToken.None);

        Assert.Empty(rooms);
    }

    [Fact]
    public async Task Discovery_lists_a_restricted_room_with_HasAccess_false_for_a_non_owner()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        await fx.CreateHandler.HandleAsync(new CreateChatRoomCommand(programId, "meditation", "Meditation", Guid.NewGuid()), CancellationToken.None);
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        fx.ProgramAccessContext.GrantAccess(ownerId, programId);

        var ownerView = await fx.ListRoomsHandler.HandleAsync(ownerId, CancellationToken.None);
        var outsiderView = await fx.ListRoomsHandler.HandleAsync(outsiderId, CancellationToken.None);

        Assert.True(Assert.Single(ownerView).HasAccess);
        Assert.False(Assert.Single(outsiderView).HasAccess);
    }

    private static ChatRoom LegacyInactiveRoom(string key, string name)
    {
        var room = ChatRoom.Create(Guid.NewGuid(), key, name, null);
        // Immediately deactivate to mirror the migration-seeded legacy rows (which are inserted
        // directly as inactive with a null ProgramId, never through Create — reproduced here via
        // the same public Deactivate mutator instead of reflection).
        room.Deactivate(Guid.NewGuid());
        return room;
    }
}
