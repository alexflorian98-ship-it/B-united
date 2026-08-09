using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

/// <summary>P6.05/P6.13 — cursor-paginated room history, newest page first (`beforeUtc` is the
/// oldest-loaded message's timestamp; omit it for the initial/most-recent page). A deleted
/// message's <see cref="MessageDto.Body"/> is returned as null — its removal is visible (the
/// slot stays, so pinned/ordering context isn't lost) but the original text is not exposed to
/// ordinary room members, only to moderators via the admin report queue.
///
/// docs/TASKS.md P3.43.a: the room's owning program is resolved server-side and gated with
/// <see cref="IProgramAccessContext.RequireProgramAccessAsync"/> before any history is
/// returned.</summary>
public sealed class GetMessagesHandler(DbContext dbContext, IUserLookup userLookup, IProgramAccessContext programAccessContext)
{
    private const int PageSize = 50;

    public async Task<MessagePageResult> HandleAsync(Guid roomId, Guid userId, DateTime? beforeUtc, CancellationToken cancellationToken)
    {
        var room = await dbContext.Set<ChatRoom>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundAppException("The specified room does not exist.");

        if (room.ProgramId is not null)
        {
            await programAccessContext.RequireProgramAccessAsync(userId, room.ProgramId.Value, cancellationToken);
        }

        var query = dbContext.Set<Message>().AsNoTracking().Where(m => m.RoomId == roomId);
        if (beforeUtc.HasValue)
        {
            query = query.Where(m => m.CreatedAt < beforeUtc.Value);
        }

        var page = await query.OrderByDescending(m => m.CreatedAt).Take(PageSize).ToListAsync(cancellationToken);
        page.Reverse();

        var userIds = page.Select(m => m.UserId).Distinct().ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        var items = page.Select(m => new MessageDto(
            m.Id,
            m.UserId,
            users.TryGetValue(m.UserId, out var user) ? user.Email : null,
            m.IsDeleted ? null : m.Body,
            m.IsPinned,
            m.IsDeleted,
            m.CreatedAt)).ToList();

        var nextCursor = page.Count == PageSize ? page[0].CreatedAt : (DateTime?)null;
        return new MessagePageResult(items, nextCursor);
    }
}
