using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

/// <summary>docs/TASKS.md P3.43.a: the room's owning program is resolved server-side and gated
/// with <see cref="IProgramAccessContext.RequireProgramAccessAsync"/> before the read-state is
/// updated.</summary>
public sealed class MarkRoomReadHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task HandleAsync(Guid roomId, Guid userId, CancellationToken cancellationToken)
    {
        var room = await dbContext.Set<ChatRoom>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundAppException("The specified room does not exist.");

        if (room.ProgramId is not null)
        {
            await programAccessContext.RequireProgramAccessAsync(userId, room.ProgramId.Value, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;
        var state = await dbContext.Set<ChatReadState>().FirstOrDefaultAsync(r => r.UserId == userId && r.RoomId == roomId, cancellationToken);

        if (state is null)
        {
            dbContext.Set<ChatReadState>().Add(ChatReadState.Create(userId, roomId, utcNow));
        }
        else
        {
            state.MarkRead(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
