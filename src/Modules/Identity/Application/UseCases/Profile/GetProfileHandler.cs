using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Profile;

public sealed class GetProfileHandler(DbContext dbContext)
{
    public async Task<ProfileResult> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>().SingleAsync(u => u.Id == userId, cancellationToken);
        var preference = await dbContext.Set<UserPreference>().SingleAsync(p => p.UserId == userId, cancellationToken);

        return new ProfileResult(user.Id, user.Email, preference.Timezone, preference.PreferredLanguage, preference.EmailNotificationsEnabled);
    }
}
