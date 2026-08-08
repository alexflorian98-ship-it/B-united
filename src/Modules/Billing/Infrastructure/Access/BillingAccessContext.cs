using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Infrastructure.Access;

/// <summary>The real <see cref="IAccessContext"/> implementation (P3.15), replacing the P2.09
/// <c>StubAccessContext</c> that always granted access. Billing is the sole owner of this
/// decision (CLAUDE.md) — every other module consumes this interface, never
/// <c>Entitlement</c>/<c>Subscription</c> directly.</summary>
public sealed class BillingAccessContext(DbContext dbContext) : IAccessContext
{
    public async Task<bool> HasActivePlatformAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entitlement = await dbContext.Set<Entitlement>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == userId && e.Type == Entitlement.PlatformAccessType, cancellationToken);

        return entitlement is not null && entitlement.IsActiveAt(DateTime.UtcNow);
    }
}
