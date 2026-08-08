using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed class DeleteSectionHandler(DbContext dbContext)
{
    public async Task HandleAsync(Guid sectionId, CancellationToken cancellationToken)
    {
        var section = await dbContext.Set<Section>().SingleOrDefaultAsync(s => s.Id == sectionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified section does not exist.");

        // Cascades to SectionTranslation/ContentItem/ContentItemTranslation via FK configuration.
        dbContext.Set<Section>().Remove(section);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
