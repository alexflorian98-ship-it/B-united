using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class GetProgramDetailHandler(DbContext dbContext)
{
    public async Task<ProgramDetailDto> HandleAsync(Guid programId, CancellationToken cancellationToken)
    {
        var program = await dbContext.Set<Program>().SingleOrDefaultAsync(p => p.Id == programId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist.");

        var translations = await dbContext.Set<ProgramTranslation>()
            .Where(t => t.ProgramId == programId)
            .Select(t => new ProgramTranslationDto(t.Language, t.Title, t.ShortDescription, t.Description))
            .ToListAsync(cancellationToken);

        var sections = await dbContext.Set<Section>()
            .Where(s => s.ProgramId == programId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);
        var sectionIds = sections.Select(s => s.Id).ToList();

        var sectionTranslations = await dbContext.Set<SectionTranslation>()
            .Where(t => sectionIds.Contains(t.SectionId))
            .ToListAsync(cancellationToken);

        var items = await dbContext.Set<ContentItem>()
            .Where(c => sectionIds.Contains(c.SectionId))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(i => i.Id).ToList();

        var itemTranslations = await dbContext.Set<ContentItemTranslation>()
            .Where(t => itemIds.Contains(t.ContentItemId))
            .ToListAsync(cancellationToken);

        var sectionDtos = sections.Select(section => new SectionDetailDto(
            section.Id,
            section.SortOrder,
            section.Status,
            sectionTranslations.Where(t => t.SectionId == section.Id)
                .Select(t => new SectionTranslationDto(t.Language, t.Title, t.Description))
                .ToList(),
            items.Where(i => i.SectionId == section.Id)
                .Select(item => new ContentItemDetailDto(
                    item.Id,
                    item.Type,
                    item.SortOrder,
                    item.IsRequired,
                    item.MediaAssetId,
                    itemTranslations.Where(t => t.ContentItemId == item.Id)
                        .Select(t => new ContentItemTranslationDto(t.Language, t.Title, t.Body))
                        .ToList()))
                .ToList()))
            .ToList();

        return new ProgramDetailDto(
            program.Id,
            program.DomainId,
            program.Slug,
            program.Status,
            program.DefaultLanguage,
            program.CoverAssetId,
            program.SortOrder,
            translations,
            sectionDtos);
    }
}
