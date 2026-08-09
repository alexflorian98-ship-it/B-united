using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Admin;

/// <summary>docs/TASKS.md P3.43.a — admin-only rename/activate/deactivate. The room's
/// <see cref="ChatRoom.ProgramId"/> association is immutable after creation (create a new room
/// instead of repurposing one) — this is intentional: reassigning an existing room would silently
/// change entitlement scope for all its existing history.</summary>
public sealed class UpdateChatRoomHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpdateChatRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await dbContext.Set<ChatRoom>().FirstOrDefaultAsync(r => r.Id == command.RoomId, cancellationToken)
            ?? throw new NotFoundAppException("The specified room does not exist.");

        room.Rename(command.Name, command.UpdatedBy);

        if (command.IsActive)
        {
            room.Activate(command.UpdatedBy);
        }
        else
        {
            room.Deactivate(command.UpdatedBy);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
