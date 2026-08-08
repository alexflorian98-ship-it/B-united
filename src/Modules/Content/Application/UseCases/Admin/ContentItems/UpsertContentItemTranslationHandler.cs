using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed class UpsertContentItemTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertContentItemTranslationCommand command, CancellationToken cancellationToken)
    {
        var itemExists = await dbContext.Set<ContentItem>().AnyAsync(c => c.Id == command.ContentItemId, cancellationToken);
        if (!itemExists)
        {
            throw new NotFoundAppException("The specified content item does not exist.");
        }

        var translation = await dbContext.Set<ContentItemTranslation>()
            .SingleOrDefaultAsync(t => t.ContentItemId == command.ContentItemId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            dbContext.Set<ContentItemTranslation>().Add(
                ContentItemTranslation.Create(command.ContentItemId, command.Language, command.Title, command.Body));
        }
        else
        {
            translation.Update(command.Title, command.Body);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
