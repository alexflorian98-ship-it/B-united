using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed class RecordVideoProgressHandler(DbContext dbContext, TimeProvider timeProvider)
{
    public async Task HandleAsync(RecordVideoProgressCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var progress = await dbContext.Set<ContentProgress>()
            .SingleOrDefaultAsync(p => p.UserId == command.UserId && p.ContentItemId == command.ContentItemId, cancellationToken);

        if (progress is null)
        {
            progress = ContentProgress.Start(command.UserId, command.ContentItemId);
            dbContext.Set<ContentProgress>().Add(progress);
        }

        progress.RecordVideoPosition(command.PositionSeconds, command.WatchPercentage, utcNow);

        // Flushed before recalculating: the recalculation counts completed items via a database
        // query, which wouldn't see this row's change until it's actually saved.
        await dbContext.SaveChangesAsync(cancellationToken);

        await SectionProgressRecalculator.RecalculateAsync(dbContext, command.UserId, command.SectionId, command.SectionContentItemIds, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
