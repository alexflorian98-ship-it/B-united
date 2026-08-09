using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Progress.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Infrastructure.CrossModule;

/// <summary>
/// Implements Progress's <see cref="IContentItemProgramLookup"/> — resolves
/// <c>ContentItem -> Section -> Program</c> (or directly <c>Section -> Program</c>) so Progress
/// can gate its 4 progress-tracking handlers on per-program ownership without referencing
/// Content's Domain/Infrastructure directly (CLAUDE.md). Mirrors <see cref="ProgramLookup"/>'s
/// read-only, <c>AsNoTracking</c> style.
/// </summary>
public sealed class ContentItemProgramLookup(DbContext dbContext) : IContentItemProgramLookup
{
    public Task<Guid?> GetOwningProgramIdForContentItemAsync(Guid contentItemId, CancellationToken cancellationToken) =>
        dbContext.Set<ContentItem>().AsNoTracking()
            .Where(item => item.Id == contentItemId)
            .Join(
                dbContext.Set<Section>().AsNoTracking(),
                item => item.SectionId,
                section => section.Id,
                (item, section) => (Guid?)section.ProgramId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Guid?> GetOwningProgramIdForSectionAsync(Guid sectionId, CancellationToken cancellationToken) =>
        dbContext.Set<Section>().AsNoTracking()
            .Where(section => section.Id == sectionId)
            .Select(section => (Guid?)section.ProgramId)
            .SingleOrDefaultAsync(cancellationToken);
}
