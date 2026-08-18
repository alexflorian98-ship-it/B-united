using BUnited.Modules.Chat.Application.UseCases.Provisioning;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Tests.TestSupport;

namespace BUnited.Modules.Chat.Tests.Application;

/// <summary>The Content module's real cross-module dependency (see
/// <c>ProgramStatusHandler.PublishAsync</c>) — proves the room this contract creates is actually
/// named after the program and reachable through the normal client room list.</summary>
public sealed class ProgramChatRoomProvisionerTests
{
    [Fact]
    public async Task First_call_for_a_program_creates_a_room_named_after_the_program()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        var programId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var provisioner = new ProgramChatRoomProvisioner(context);

        await provisioner.EnsureRoomForProgramAsync(programId, "Trai constient", actorId, CancellationToken.None);

        var room = Assert.Single(context.Set<ChatRoom>());
        Assert.Equal(programId, room.ProgramId);
        Assert.Equal("Trai constient", room.Name);
        Assert.True(room.IsActive);
    }

    [Fact]
    public async Task A_second_call_for_the_same_program_does_not_create_a_duplicate_room()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        var programId = Guid.NewGuid();
        var provisioner = new ProgramChatRoomProvisioner(context);

        await provisioner.EnsureRoomForProgramAsync(programId, "Trai constient", Guid.NewGuid(), CancellationToken.None);
        await provisioner.EnsureRoomForProgramAsync(programId, "Trai constient", Guid.NewGuid(), CancellationToken.None);

        Assert.Single(context.Set<ChatRoom>());
    }

    [Fact]
    public async Task A_deliberately_deactivated_room_is_never_silently_recreated()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        var programId = Guid.NewGuid();
        var provisioner = new ProgramChatRoomProvisioner(context);

        await provisioner.EnsureRoomForProgramAsync(programId, "Trai constient", Guid.NewGuid(), CancellationToken.None);
        var room = Assert.Single(context.Set<ChatRoom>());
        room.Deactivate(Guid.NewGuid());
        await context.SaveChangesAsync();

        await provisioner.EnsureRoomForProgramAsync(programId, "Trai constient", Guid.NewGuid(), CancellationToken.None);

        var onlyRoom = Assert.Single(context.Set<ChatRoom>());
        Assert.False(onlyRoom.IsActive);
    }
}
