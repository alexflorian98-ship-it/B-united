using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

/// <summary>P6.12 — the room switcher's data: last message preview + basic unread state (P6.06)
/// per room. Only active rooms are listed (the 6 deactivated legacy rooms from before
/// program-scoping — docs/TASKS.md P3.43.a — never appear here or anywhere else client-facing).
/// Listing itself stays open to every <c>chat.use</c> caller; <see cref="RoomSummaryDto.HasAccess"/>
/// marks which rooms are actually joinable so the client can show a locked state rather than the
/// room being hidden outright.</summary>
public sealed class ListRoomsHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<IReadOnlyList<RoomSummaryDto>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rooms = await dbContext.Set<ChatRoom>().AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
        var roomIds = rooms.Select(r => r.Id).ToList();

        var lastMessages = await dbContext.Set<Message>().AsNoTracking()
            .Where(m => roomIds.Contains(m.RoomId) && !m.IsDeleted)
            .GroupBy(m => m.RoomId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToListAsync(cancellationToken);

        var readStates = await dbContext.Set<ChatReadState>().AsNoTracking()
            .Where(r => r.UserId == userId && roomIds.Contains(r.RoomId))
            .ToListAsync(cancellationToken);

        var result = new List<RoomSummaryDto>();
        foreach (var room in rooms)
        {
            var lastMessage = lastMessages.FirstOrDefault(m => m.RoomId == room.Id);
            var lastReadAtUtc = readStates.FirstOrDefault(r => r.RoomId == room.Id)?.LastReadAtUtc;

            var unreadCount = await dbContext.Set<Message>().AsNoTracking()
                .CountAsync(m => m.RoomId == room.Id && !m.IsDeleted && (lastReadAtUtc == null || m.CreatedAt > lastReadAtUtc), cancellationToken);

            var hasAccess = room.ProgramId is null || await programAccessContext.HasProgramAccessAsync(userId, room.ProgramId.Value, cancellationToken);

            result.Add(new RoomSummaryDto(room.Id, room.Key, room.Name, room.ProgramId, hasAccess, lastMessage?.Body, lastMessage?.CreatedAt, unreadCount));
        }

        return result;
    }
}
