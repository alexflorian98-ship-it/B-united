using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>§53 Muted Users screen.</summary>
public sealed class ListMutedUsersHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<IReadOnlyList<MutedUserSummaryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var mutes = await dbContext.Set<Mute>().AsNoTracking()
            .Where(m => m.ExpiresAtUtc > utcNow)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var userIds = mutes.Select(m => m.UserId).Concat(mutes.Select(m => m.ModeratorId)).Distinct().ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        return mutes.Select(m => new MutedUserSummaryDto(
            m.Id,
            m.UserId,
            users.TryGetValue(m.UserId, out var user) ? user.Email : null,
            m.Reason,
            m.ExpiresAtUtc,
            users.TryGetValue(m.ModeratorId, out var moderator) ? moderator.Email : null)).ToList();
    }
}
