using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Progress.Contracts;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Application.UseCases;

/// <summary>Gates the write on owning the specific program the content item belongs to — see
/// <see cref="RecordVideoProgressHandler"/>'s identical remarks.</summary>
public sealed class MarkContentCompletedHandler(
    DbContext dbContext,
    TimeProvider timeProvider,
    IContentItemProgramLookup contentItemProgramLookup,
    IProgramAccessContext programAccessContext)
{
    public async Task HandleAsync(MarkContentCompletedCommand command, CancellationToken cancellationToken)
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

        progress.MarkCompletedManually(utcNow);

        // Flushed before recalculating — see RecordVideoProgressHandler's identical comment.
        await dbContext.SaveChangesAsync(cancellationToken);

        await SectionProgressRecalculator.RecalculateAsync(dbContext, command.UserId, command.SectionId, command.SectionContentItemIds, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
