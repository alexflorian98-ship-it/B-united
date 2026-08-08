using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Profile;

public sealed class UpdateProfileHandler(DbContext dbContext, ILogger<UpdateProfileHandler> logger)
{
    public async Task<ProfileResult> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>().SingleAsync(u => u.Id == command.UserId, cancellationToken);
        var preference = await dbContext.Set<UserPreference>().SingleAsync(p => p.UserId == command.UserId, cancellationToken);

        preference.UpdateTimezone(command.Timezone);
        preference.UpdatePreferredLanguage(command.PreferredLanguage);
        preference.SetEmailNotificationsEnabled(command.EmailNotificationsEnabled);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("identity.profile_updated: UserId {UserId}", user.Id);

        return new ProfileResult(user.Id, user.Email, preference.Timezone, preference.PreferredLanguage, preference.EmailNotificationsEnabled);
    }
}
