using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A3 — real client administration replacing the
/// "Subscribers" placeholder. Deliberately self-contained within Identity: the list view needs
/// only identity metadata and role membership, neither of which requires crossing into another
/// module's tables (unlike the commerce summary on the detail page, which does — see
/// <c>BUnited.Modules.Admin.Application.UseCases.GetClientCommerceSummaryHandler</c>).</summary>
public sealed class ListClientsHandler(DbContext dbContext)
{
    public async Task<ClientListResult> HandleAsync(ListClientsQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<User>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var normalizedSearch = User.Normalize(query.Search.Trim());
            baseQuery = baseQuery.Where(u => u.NormalizedEmail.Contains(normalizedSearch));
        }

        if (query.RoleId is { } roleId)
        {
            baseQuery = baseQuery.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));
        }

        baseQuery = baseQuery.OrderBy(u => u.Email);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.IsActive,
                u.EmailVerifiedAtUtc,
                u.CreatedAt,
                Roles = u.UserRoles.Select(ur => new RoleSummaryDto(ur.Role.Id, ur.Role.Name)).ToList(),
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(u => new ClientListItemDto(u.Id, u.Email, u.IsActive, u.EmailVerifiedAtUtc is not null, u.CreatedAt, u.Roles))
            .ToList();

        return new ClientListResult(items, totalCount, query.Page, query.PageSize);
    }
}
