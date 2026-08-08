using BUnited.BuildingBlocks.Application.Errors;
using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed class GetPublishedProgramDetailHandler(DbContext dbContext)
{
    public async Task<ClientProgramDetailDto> HandleAsync(string slug, string requestedLanguage, CancellationToken cancellationToken)
    {
        var program = await dbContext.Set<Program>()
            .SingleOrDefaultAsync(p => p.Slug == slug && p.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist or is not published.");

        var programTranslations = await dbContext.Set<ProgramTranslation>()
            .Where(t => t.ProgramId == program.Id)
            .ToListAsync(cancellationToken);
        var programResolution = TranslationResolver.Resolve(programTranslations, requestedLanguage, program.DefaultLanguage);

        var sections = await dbContext.Set<Section>()
            .Where(s => s.ProgramId == program.Id && s.Status == ContentStatus.Published)
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

        var sectionDtos = sections.Select(section =>
        {
            var sectionResolution = TranslationResolver.Resolve(
                sectionTranslations.Where(t => t.SectionId == section.Id), requestedLanguage, program.DefaultLanguage);

            var itemDtos = items.Where(i => i.SectionId == section.Id).Select(item =>
            {
                var itemResolution = TranslationResolver.Resolve(
                    itemTranslations.Where(t => t.ContentItemId == item.Id), requestedLanguage, program.DefaultLanguage);

                return new ClientContentItemDto(
                    item.Id,
                    item.Type.ToString(),
                    item.SortOrder,
                    item.IsRequired,
                    itemResolution.Translation.Title,
                    itemResolution.Translation.Body,
                    item.MediaAssetId);
            }).ToList();

            return new ClientSectionDto(section.Id, section.SortOrder, sectionResolution.Translation.Title, sectionResolution.Translation.Description, itemDtos);
        }).ToList();

        return new ClientProgramDetailDto(
            program.Id,
            program.Slug,
            program.DomainId,
            programResolution.Translation.Title,
            programResolution.Translation.ShortDescription,
            programResolution.Translation.Description,
            program.CoverAssetId,
            sectionDtos);
    }
}
