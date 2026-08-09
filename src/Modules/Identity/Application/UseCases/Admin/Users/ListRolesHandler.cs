using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

/// <summary>Backs the role filter dropdown and the role-assignment control on the client detail
/// screen — the full, small, fixed set of <see cref="Role"/> rows (Client/Expert/Administrator
/// today), not paginated.</summary>
public sealed class ListRolesHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<RoleSummaryDto>> HandleAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<Role>().AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);
}
