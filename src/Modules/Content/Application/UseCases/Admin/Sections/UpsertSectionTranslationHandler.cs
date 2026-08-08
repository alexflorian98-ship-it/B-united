using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed class UpsertSectionTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertSectionTranslationCommand command, CancellationToken cancellationToken)
    {
        var sectionExists = await dbContext.Set<Section>().AnyAsync(s => s.Id == command.SectionId, cancellationToken);
        if (!sectionExists)
        {
            throw new NotFoundAppException("The specified section does not exist.");
        }

        var translation = await dbContext.Set<SectionTranslation>()
            .SingleOrDefaultAsync(t => t.SectionId == command.SectionId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            dbContext.Set<SectionTranslation>().Add(
                SectionTranslation.Create(command.SectionId, command.Language, command.Title, command.Description));
        }
        else
        {
            translation.Update(command.Title, command.Description);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
