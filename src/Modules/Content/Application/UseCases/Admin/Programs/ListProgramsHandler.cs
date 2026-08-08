using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class ListProgramsHandler(DbContext dbContext)
{
    public async Task<ProgramListResult> HandleAsync(ListProgramsQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<Program>().AsQueryable();
        if (query.Status is not null)
        {
            baseQuery = baseQuery.Where(p => p.Status == query.Status);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Slug)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var programIds = page.Select(p => p.Id).ToList();

        var translations = await dbContext.Set<ProgramTranslation>()
            .Where(t => programIds.Contains(t.ProgramId))
            .ToListAsync(cancellationToken);

        var sectionCounts = await dbContext.Set<Section>()
            .Where(s => programIds.Contains(s.ProgramId))
            .GroupBy(s => s.ProgramId)
            .Select(g => new { ProgramId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var items = page.Select(program =>
        {
            var programTranslations = translations.Where(t => t.ProgramId == program.Id).ToList();
            var defaultTitle = programTranslations.FirstOrDefault(t => t.Language == program.DefaultLanguage)?.Title
                ?? programTranslations.FirstOrDefault()?.Title
                ?? program.Slug;

            return new ProgramSummaryDto(
                program.Id,
                program.Slug,
                program.Status,
                program.DefaultLanguage,
                program.DomainId,
                program.SortOrder,
                defaultTitle,
                sectionCounts.FirstOrDefault(s => s.ProgramId == program.Id)?.Count ?? 0,
                programTranslations.Select(t => t.Language).OrderBy(l => l).ToList(),
                program.UpdatedAt);
        }).ToList();

        return new ProgramListResult(items, totalCount);
    }
}
