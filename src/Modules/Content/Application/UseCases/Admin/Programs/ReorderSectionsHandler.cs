using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class ReorderSectionsHandler(DbContext dbContext)
{
    public async Task HandleAsync(ReorderSectionsCommand command, CancellationToken cancellationToken)
    {
        var sections = await dbContext.Set<Section>()
            .Where(s => s.ProgramId == command.ProgramId)
            .ToListAsync(cancellationToken);

        var existingIds = sections.Select(s => s.Id).ToHashSet();
        if (existingIds.Count != command.OrderedSectionIds.Count || !existingIds.SetEquals(command.OrderedSectionIds))
        {
            throw new BusinessRuleAppException(
                "SECTION_REORDER_SET_MISMATCH",
                "errors.section.reorderSetMismatch",
                "The reorder request must include exactly the program's current sections, no more and no fewer.");
        }

        for (var index = 0; index < command.OrderedSectionIds.Count; index++)
        {
            sections.Single(s => s.Id == command.OrderedSectionIds[index]).Reorder(index);
        }

        // A single SaveChangesAsync commits every reordered row in one transaction — either
        // every section gets its new position or none do.
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
