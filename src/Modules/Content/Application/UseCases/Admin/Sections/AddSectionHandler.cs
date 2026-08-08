using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed class AddSectionHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(AddSectionCommand command, CancellationToken cancellationToken)
    {
        var programExists = await dbContext.Set<Program>().AnyAsync(p => p.Id == command.ProgramId, cancellationToken);
        if (!programExists)
        {
            throw new NotFoundAppException("The specified program does not exist.");
        }

        var nextSortOrder = await dbContext.Set<Section>()
            .Where(s => s.ProgramId == command.ProgramId)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var section = Section.Create(command.ProgramId, nextSortOrder + 1);
        // A section has no separate authoring workflow of its own in V1 — it's visible as soon
        // as its parent program is published. Publishing it immediately keeps ContentStatus.Draft
        // reachable-but-meaningful (Unpublish/Archive still work) rather than dead weight nothing
        // ever sets.
        section.Publish();
        dbContext.Set<Section>().Add(section);

        dbContext.Set<SectionTranslation>().Add(
            SectionTranslation.Create(section.Id, command.Language, command.Title, command.Description));

        await dbContext.SaveChangesAsync(cancellationToken);

        return section.Id;
    }
}
