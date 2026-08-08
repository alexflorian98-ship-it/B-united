using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Infrastructure.CrossModule;

public sealed class IdentityUserLookup(DbContext dbContext) : IUserLookup
{
    public async Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : new UserSummary(user.Id, user.Email, DisplayName: null);
    }

    public async Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var users = await dbContext.Set<User>().AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, u => new UserSummary(u.Id, u.Email, DisplayName: null));
    }
}
