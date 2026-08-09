using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Progress.Contracts;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

/// <summary>Gates the write on owning the specific program the content item belongs to (ADR-003:
/// per-program purchases, not a global subscription) — resolves the owning program server-side via
/// <see cref="IContentItemProgramLookup"/> and defers to <see cref="IProgramAccessContext"/>,
/// never trusting the caller-supplied <c>ContentItemId</c> alone as proof of access.</summary>
public sealed class RecordVideoProgressHandler(
    DbContext dbContext,
    TimeProvider timeProvider,
    IContentItemProgramLookup contentItemProgramLookup,
    IProgramAccessContext programAccessContext)
{
    public async Task HandleAsync(RecordVideoProgressCommand command, CancellationToken cancellationToken)
    {
        var programId = await contentItemProgramLookup.GetOwningProgramIdForContentItemAsync(command.ContentItemId, cancellationToken)
            ?? throw new NotFoundAppException("The specified content item does not exist.");

        await programAccessContext.RequireProgramAccessAsync(command.UserId, programId, cancellationToken);

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
