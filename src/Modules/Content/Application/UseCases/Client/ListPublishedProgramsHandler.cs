using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed class ListPublishedProgramsHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<ClientProgramSummaryDto>> HandleAsync(ListPublishedProgramsQuery query, CancellationToken cancellationToken)
    {
        var programsQuery = dbContext.Set<Program>().Where(p => p.Status == ContentStatus.Published);
        if (query.DomainId is not null)
        {
            programsQuery = programsQuery.Where(p => p.DomainId == query.DomainId);
        }

        var programs = await programsQuery.OrderBy(p => p.SortOrder).ToListAsync(cancellationToken);
        var programIds = programs.Select(p => p.Id).ToList();

        var translations = await dbContext.Set<ProgramTranslation>()
            .Where(t => programIds.Contains(t.ProgramId))
            .ToListAsync(cancellationToken);

        return programs.Select(program =>
        {
            var resolution = TranslationResolver.Resolve(
                translations.Where(t => t.ProgramId == program.Id),
                query.RequestedLanguage,
                program.DefaultLanguage);

            return new ClientProgramSummaryDto(
                program.Id,
                program.Slug,
                program.DomainId,
                resolution.Translation.Title,
                resolution.Translation.ShortDescription,
                program.CoverAssetId,
                program.SortOrder);
        }).ToList();
    }
}
