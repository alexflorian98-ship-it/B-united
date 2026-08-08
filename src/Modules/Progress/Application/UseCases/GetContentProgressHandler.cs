using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed record ContentProgressDto(Guid ContentItemId, string Status, int? LastVideoPositionSeconds, double? WatchPercentage);

public sealed class GetContentProgressHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<ContentProgressDto>> HandleAsync(Guid userId, IReadOnlyList<Guid> contentItemIds, CancellationToken cancellationToken) =>
        await dbContext.Set<ContentProgress>()
            .Where(p => p.UserId == userId && contentItemIds.Contains(p.ContentItemId))
            .Select(p => new ContentProgressDto(p.ContentItemId, p.Status.ToString(), p.LastVideoPositionSeconds, p.WatchPercentage))
            .ToListAsync(cancellationToken);
}
