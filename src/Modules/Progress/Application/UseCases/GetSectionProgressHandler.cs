using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Progress.Contracts;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed record SectionProgressDto(Guid SectionId, string Status, int CompletedItemCount, int TotalItemCount);

/// <summary>Gates each read on owning the specific program the section belongs to — see
/// <see cref="GetContentProgressHandler"/>'s identical remarks (same skip-unknown /
/// dedupe-per-program approach, resolved via section instead of content item).</summary>
public sealed class GetSectionProgressHandler(
    DbContext dbContext,
    IContentItemProgramLookup contentItemProgramLookup,
    IProgramAccessContext programAccessContext)
{
    public async Task<IReadOnlyList<SectionProgressDto>> HandleAsync(Guid userId, IReadOnlyList<Guid> sectionIds, CancellationToken cancellationToken)
    {
        var authorizedSectionIds = await ResolveAuthorizedSectionIdsAsync(userId, sectionIds, cancellationToken);

        return await dbContext.Set<SectionProgress>()
            .Where(p => p.UserId == userId && authorizedSectionIds.Contains(p.SectionId))
            .Select(p => new SectionProgressDto(p.SectionId, p.Status.ToString(), p.CompletedItemCount, p.TotalItemCount))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> ResolveAuthorizedSectionIdsAsync(Guid userId, IReadOnlyList<Guid> sectionIds, CancellationToken cancellationToken)
    {
        var authorizedSectionIds = new List<Guid>();
        var checkedProgramIds = new HashSet<Guid>();

        foreach (var sectionId in sectionIds.Distinct())
        {
            var programId = await contentItemProgramLookup.GetOwningProgramIdForSectionAsync(sectionId, cancellationToken);
            if (programId is null)
            {
                continue;
            }

            if (checkedProgramIds.Add(programId.Value))
            {
                await programAccessContext.RequireProgramAccessAsync(userId, programId.Value, cancellationToken);
            }

            authorizedSectionIds.Add(sectionId);
        }

        return authorizedSectionIds;
    }
}
