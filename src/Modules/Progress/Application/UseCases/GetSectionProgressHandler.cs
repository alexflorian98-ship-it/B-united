using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed record SectionProgressDto(Guid SectionId, string Status, int CompletedItemCount, int TotalItemCount);

public sealed class GetSectionProgressHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<SectionProgressDto>> HandleAsync(Guid userId, IReadOnlyList<Guid> sectionIds, CancellationToken cancellationToken) =>
        await dbContext.Set<SectionProgress>()
            .Where(p => p.UserId == userId && sectionIds.Contains(p.SectionId))
            .Select(p => new SectionProgressDto(p.SectionId, p.Status.ToString(), p.CompletedItemCount, p.TotalItemCount))
            .ToListAsync(cancellationToken);
}
