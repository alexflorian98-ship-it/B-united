using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed class ReorderContentItemsHandler(DbContext dbContext)
{
    public async Task HandleAsync(ReorderContentItemsCommand command, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<ContentItem>()
            .Where(c => c.SectionId == command.SectionId)
            .ToListAsync(cancellationToken);

        var existingIds = items.Select(i => i.Id).ToHashSet();
        if (existingIds.Count != command.OrderedContentItemIds.Count || !existingIds.SetEquals(command.OrderedContentItemIds))
        {
            throw new BusinessRuleAppException(
                "CONTENT_ITEM_REORDER_SET_MISMATCH",
                "errors.contentItem.reorderSetMismatch",
                "The reorder request must include exactly the section's current content items, no more and no fewer.");
        }

        for (var index = 0; index < command.OrderedContentItemIds.Count; index++)
        {
            items.Single(i => i.Id == command.OrderedContentItemIds[index]).Reorder(index);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
