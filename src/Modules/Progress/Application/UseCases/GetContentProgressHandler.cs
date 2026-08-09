using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Progress.Contracts;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed record ContentProgressDto(Guid ContentItemId, string Status, int? LastVideoPositionSeconds, double? WatchPercentage);

/// <summary>Gates each read on owning the specific program the content item belongs to — see
/// <see cref="RecordVideoProgressHandler"/>'s identical remarks. A content item id that does not
/// resolve to any program is silently skipped (matches the underlying query, which would already
/// never return a row for an unknown id); each id that does resolve to a program is validated with
/// <see cref="IProgramAccessContext.RequireProgramAccessAsync"/> before any progress row for it is
/// read, deduplicated per distinct program to avoid redundant checks.</summary>
public sealed class GetContentProgressHandler(
    DbContext dbContext,
    IContentItemProgramLookup contentItemProgramLookup,
    IProgramAccessContext programAccessContext)
{
    public async Task<IReadOnlyList<ContentProgressDto>> HandleAsync(Guid userId, IReadOnlyList<Guid> contentItemIds, CancellationToken cancellationToken)
    {
        var authorizedContentItemIds = await ResolveAuthorizedContentItemIdsAsync(userId, contentItemIds, cancellationToken);

        return await dbContext.Set<ContentProgress>()
            .Where(p => p.UserId == userId && authorizedContentItemIds.Contains(p.ContentItemId))
            .Select(p => new ContentProgressDto(p.ContentItemId, p.Status.ToString(), p.LastVideoPositionSeconds, p.WatchPercentage))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> ResolveAuthorizedContentItemIdsAsync(Guid userId, IReadOnlyList<Guid> contentItemIds, CancellationToken cancellationToken)
    {
        var authorizedContentItemIds = new List<Guid>();
        var checkedProgramIds = new HashSet<Guid>();

        foreach (var contentItemId in contentItemIds.Distinct())
        {
            var programId = await contentItemProgramLookup.GetOwningProgramIdForContentItemAsync(contentItemId, cancellationToken);
            if (programId is null)
            {
                continue;
            }

            if (checkedProgramIds.Add(programId.Value))
            {
                await programAccessContext.RequireProgramAccessAsync(userId, programId.Value, cancellationToken);
            }

            authorizedContentItemIds.Add(contentItemId);
        }

        return authorizedContentItemIds;
    }
}
