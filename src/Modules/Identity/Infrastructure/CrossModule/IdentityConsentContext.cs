using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Infrastructure.CrossModule;

public sealed class IdentityConsentContext(DbContext dbContext, TimeProvider timeProvider) : IConsentContext
{
    public async Task<bool> HasConsentedAsync(Guid userId, string consentType, int requiredVersion, CancellationToken cancellationToken) =>
        await dbContext.Set<UserConsent>().AsNoTracking()
            .AnyAsync(
                c => c.UserId == userId && c.ConsentType == consentType && c.Version >= requiredVersion,
                cancellationToken);

    public async Task RecordConsentAsync(Guid userId, string consentType, int version, CancellationToken cancellationToken)
    {
        dbContext.Set<UserConsent>().Add(UserConsent.Record(userId, consentType, version, timeProvider.GetUtcNow().UtcDateTime));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
