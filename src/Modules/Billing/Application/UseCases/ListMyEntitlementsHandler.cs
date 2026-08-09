using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class ListMyEntitlementsHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<ProgramEntitlementDto>> HandleAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<ProgramEntitlement>().AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.GrantedAtUtc)
            .Select(e => new ProgramEntitlementDto(e.ProgramId, e.Status.ToString(), e.GrantedAtUtc, e.RevokedAtUtc))
            .ToListAsync(cancellationToken);
}
