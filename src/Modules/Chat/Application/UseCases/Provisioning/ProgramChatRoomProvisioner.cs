using BUnited.Modules.Chat.Contracts;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Provisioning;

/// <summary>See <see cref="IProgramChatRoomProvisioner"/>. The room's key is derived from the
/// program id itself (not the program's name/slug) precisely so it can never collide with the 6
/// bare-keyed legacy rooms (docs/TASKS.md P3.43.a) or with each other across programs.</summary>
public sealed class ProgramChatRoomProvisioner(DbContext dbContext) : IProgramChatRoomProvisioner
{
    public async Task EnsureRoomForProgramAsync(Guid programId, string programName, Guid? createdBy, CancellationToken cancellationToken)
    {
        var alreadyProvisioned = await dbContext.Set<ChatRoom>().AnyAsync(r => r.ProgramId == programId, cancellationToken);
        if (alreadyProvisioned)
        {
            return;
        }

        var room = ChatRoom.Create(programId, $"program-{programId:N}", programName, createdBy);
        dbContext.Set<ChatRoom>().Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
