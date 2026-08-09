using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

/// <summary>docs/TASKS.md P3.43.b — full-replace admin management of an event's program
/// association(s). Each supplied <see cref="SetEventProgramsCommand.ProgramIds"/> entry is
/// validated against Content via <see cref="IProgramLookup"/> (a Contracts-only cross-module
/// read, never Content's Domain/Infrastructure — CLAUDE.md) and must reference a published
/// program, mirroring <c>CreateProgramOfferHandler</c>/<c>CreateQuestionnaireHandler</c> for
/// consistency. An empty list clears every association, reverting the event to
/// public-to-all-authenticated registration.</summary>
public sealed class SetEventProgramsHandler(DbContext dbContext, IProgramLookup programLookup)
{
    public async Task HandleAsync(SetEventProgramsCommand command, CancellationToken cancellationToken)
    {
        var eventExists = await dbContext.Set<Event>().AnyAsync(e => e.Id == command.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundAppException("The specified event does not exist.");
        }

        var distinctProgramIds = command.ProgramIds.Distinct().ToList();

        foreach (var programId in distinctProgramIds)
        {
            var program = await programLookup.GetProgramAsync(programId, cancellationToken)
                ?? throw new NotFoundAppException($"Program {programId} does not exist.");

            if (program.Status != ProgramLookupStatus.Published)
            {
                throw new BusinessRuleAppException(
                    "EVENT_PROGRAM_NOT_PUBLISHED",
                    "errors.event.programNotPublished",
                    "An event can only be associated with a published program.");
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existing = await dbContext.Set<EventProgram>()
            .Where(ep => ep.EventId == command.EventId)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(ep => !distinctProgramIds.Contains(ep.ProgramId)).ToList();
        dbContext.Set<EventProgram>().RemoveRange(toRemove);

        var existingProgramIds = existing.Select(ep => ep.ProgramId).ToHashSet();
        var toAdd = distinctProgramIds
            .Where(programId => !existingProgramIds.Contains(programId))
            .Select(programId => EventProgram.Create(command.EventId, programId));
        dbContext.Set<EventProgram>().AddRange(toAdd);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
