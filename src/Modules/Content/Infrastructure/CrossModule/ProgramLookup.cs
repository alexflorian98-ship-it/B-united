using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Infrastructure.CrossModule;

public sealed class ProgramLookup(DbContext dbContext) : IProgramLookup
{
    public async Task<ProgramSummary?> GetProgramAsync(Guid programId, CancellationToken cancellationToken)
    {
        var program = await dbContext.Set<Program>().AsNoTracking()
            .Where(p => p.Id == programId)
            .Select(p => new { p.Id, p.Slug, p.Status, p.DefaultLanguage })
            .SingleOrDefaultAsync(cancellationToken);

        if (program is null)
        {
            return null;
        }

        var title = await dbContext.Set<ProgramTranslation>().AsNoTracking()
            .Where(t => t.ProgramId == programId && t.Language == program.DefaultLanguage)
            .Select(t => t.Title)
            .SingleOrDefaultAsync(cancellationToken);

        return new ProgramSummary(program.Id, program.Slug, MapStatus(program.Status), program.DefaultLanguage, title);
    }

    private static ProgramLookupStatus MapStatus(ContentStatus status) => status switch
    {
        ContentStatus.Draft => ProgramLookupStatus.Draft,
        ContentStatus.Published => ProgramLookupStatus.Published,
        ContentStatus.Archived => ProgramLookupStatus.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown content status."),
    };
}
