using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Admin;

/// <summary>docs/TASKS.md P3.43.a — admin room management list, including inactive/legacy rows
/// (unlike <see cref="Client.ListRoomsHandler"/>, which only ever shows active rooms to
/// clients).</summary>
public sealed class ListChatRoomsAdminHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<ChatRoomAdminDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var rooms = await dbContext.Set<ChatRoom>().AsNoTracking()
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rooms.Select(r => new ChatRoomAdminDto(r.Id, r.ProgramId, r.Key, r.Name, r.IsActive, r.CreatedAt)).ToList();
    }
}
