using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Infrastructure.CrossModule;

public sealed class IdentityNotificationPreferenceLookup(DbContext dbContext) : INotificationPreferenceLookup
{
    public async Task<bool> AreEmailNotificationsEnabledAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await dbContext.Set<UserPreference>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        // No preference row yet defaults to enabled (UserPreference.CreateDefault's own default).
        return preference?.EmailNotificationsEnabled ?? true;
    }
}
