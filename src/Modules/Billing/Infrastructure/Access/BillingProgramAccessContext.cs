using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Infrastructure.Access;

/// <summary>The real <see cref="IProgramAccessContext"/> implementation (replaces the old
/// <c>BillingAccessContext</c>/<c>IAccessContext</c> global check). Billing is the sole owner of
/// this decision (CLAUDE.md) — every other module consumes this interface, never
/// <c>ProgramEntitlement</c> directly.</summary>
public sealed class BillingProgramAccessContext(DbContext dbContext) : IProgramAccessContext
{
    public async Task<bool> HasProgramAccessAsync(Guid userId, Guid programId, CancellationToken cancellationToken)
    {
        var entitlement = await dbContext.Set<ProgramEntitlement>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == userId && e.ProgramId == programId, cancellationToken);

        return entitlement is not null && entitlement.Status == ProgramEntitlementStatus.Active;
    }

    public async Task<IReadOnlySet<Guid>> GetAccessibleProgramIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken)
    {
        if (programIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var accessible = await dbContext.Set<ProgramEntitlement>().AsNoTracking()
            .Where(e => e.UserId == userId && programIds.Contains(e.ProgramId) && e.Status == ProgramEntitlementStatus.Active)
            .Select(e => e.ProgramId)
            .ToListAsync(cancellationToken);

        return new HashSet<Guid>(accessible);
    }
}
