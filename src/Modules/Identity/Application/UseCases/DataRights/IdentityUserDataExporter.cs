using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.DataRights;

/// <summary>Identity's own section of the "export my data" archive: profile, preferences, and
/// full consent history. Registered as an <see cref="IUserDataExporter"/> alongside every other
/// module so <see cref="ExportMyDataHandler"/> treats Identity's own data the same way it treats
/// every other module's (docs/DATA_RETENTION_POLICY.md).</summary>
public sealed class IdentityUserDataExporter(DbContext dbContext) : IUserDataExporter
{
    public string SectionKey => "identity";

    public async Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email, u.CreatedAt, u.EmailVerifiedAtUtc })
            .SingleOrDefaultAsync(cancellationToken);

        var preference = await dbContext.Set<UserPreference>().AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.Timezone, p.PreferredLanguage, p.EmailNotificationsEnabled })
            .SingleOrDefaultAsync(cancellationToken);

        var consents = await dbContext.Set<UserConsent>().AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.ConsentedAtUtc)
            .Select(c => new { c.ConsentType, c.Version, c.ConsentedAtUtc })
            .ToListAsync(cancellationToken);

        return new
        {
            Profile = user,
            Preferences = preference,
            Consents = consents,
        };
    }
}
