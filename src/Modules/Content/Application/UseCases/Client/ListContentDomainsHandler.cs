using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed record ContentDomainDto(Guid Id, string Slug, int SortOrder);

public sealed class ListContentDomainsHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<ContentDomainDto>> HandleAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<ContentDomain>()
            .OrderBy(d => d.SortOrder)
            .Select(d => new ContentDomainDto(d.Id, d.Slug, d.SortOrder))
            .ToListAsync(cancellationToken);
}
