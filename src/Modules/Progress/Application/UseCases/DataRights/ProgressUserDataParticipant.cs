using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases.DataRights;

/// <summary>Progress's export/erasure participants (docs/DATA_RETENTION_POLICY.md): the user's
/// own learning history has no third-party or legal retention interest, so account deletion hard
/// deletes both tables.</summary>
public sealed class ProgressUserDataExporter(DbContext dbContext) : IUserDataExporter
{
    public string SectionKey => "progress";

    public async Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var contentProgress = await dbContext.Set<ContentProgress>().AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.ContentItemId,
                Status = p.Status.ToString(),
                p.LastVideoPositionSeconds,
                p.WatchPercentage,
                p.StartedAt,
                p.CompletedAt,
            })
            .ToListAsync(cancellationToken);

        var sectionProgress = await dbContext.Set<SectionProgress>().AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.SectionId,
                Status = p.Status.ToString(),
                p.CompletedItemCount,
                p.TotalItemCount,
            })
            .ToListAsync(cancellationToken);

        return new { ContentProgress = contentProgress, SectionProgress = sectionProgress };
    }
}

public sealed class ProgressUserDataEraser(DbContext dbContext) : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var contentProgress = await dbContext.Set<ContentProgress>().Where(p => p.UserId == userId).ToListAsync(cancellationToken);
        dbContext.Set<ContentProgress>().RemoveRange(contentProgress);

        var sectionProgress = await dbContext.Set<SectionProgress>().Where(p => p.UserId == userId).ToListAsync(cancellationToken);
        dbContext.Set<SectionProgress>().RemoveRange(sectionProgress);
    }
}
