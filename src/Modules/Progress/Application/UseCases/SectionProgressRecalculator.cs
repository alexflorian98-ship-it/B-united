using BUnited.Modules.Progress.Domain;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

/// <summary>Shared by every use case that changes a <see cref="ContentProgress"/> row —
/// recalculates the owning section's denormalized <see cref="SectionProgress"/> cache from the
/// caller-supplied list of the section's content item IDs (this module never looks that
/// structure up itself, see <see cref="ContentProgress"/>'s remarks).</summary>
internal static class SectionProgressRecalculator
{
    public static async Task RecalculateAsync(
        DbContext dbContext,
        Guid userId,
        Guid sectionId,
        IReadOnlyCollection<Guid> sectionContentItemIds,
        CancellationToken cancellationToken)
    {
        var completedCount = await dbContext.Set<ContentProgress>()
            .CountAsync(
                p => p.UserId == userId && sectionContentItemIds.Contains(p.ContentItemId) && p.Status == ContentProgressStatus.Completed,
                cancellationToken);

        var sectionProgress = await dbContext.Set<SectionProgress>()
            .SingleOrDefaultAsync(p => p.UserId == userId && p.SectionId == sectionId, cancellationToken);

        if (sectionProgress is null)
        {
            sectionProgress = SectionProgress.Create(userId, sectionId);
            dbContext.Set<SectionProgress>().Add(sectionProgress);
        }

        sectionProgress.Recalculate(completedCount, sectionContentItemIds.Count);
    }
}
