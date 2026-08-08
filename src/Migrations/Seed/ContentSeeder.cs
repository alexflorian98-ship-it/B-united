using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Migrations.Seed;

/// <summary>Idempotent seed for the 5 initial content domains (docs/PROMPT.md §18–22, P2.01).
/// Safe to run on every application startup — guarded by an existence check against the fixed
/// <see cref="WellKnownContentDomains"/> IDs, same pattern as <see cref="IdentitySeeder"/>.</summary>
public static class ContentSeeder
{
    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var existingIds = (await context.ContentDomains.Select(d => d.Id).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var (id, slug, sortOrder) in WellKnownContentDomains.All)
        {
            if (existingIds.Add(id))
            {
                context.ContentDomains.Add(ContentDomain.Create(id, slug, sortOrder));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
