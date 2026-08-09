using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Billing.Contracts;
using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Client;

/// <summary>P3.37.a — the catalogue view. <see cref="IProgramOfferLookup"/> and
/// <see cref="IProgramAccessContext"/> are Billing's Contracts-only cross-module surfaces
/// (CLAUDE.md: never reference another module's Domain/Infrastructure) so a program's active
/// price and the caller's ownership can be shown without Content owning any commerce data.</summary>
public sealed class ListPublishedProgramsHandler(DbContext dbContext, IProgramOfferLookup programOfferLookup, IProgramAccessContext programAccessContext)
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

        var activeOffers = await programOfferLookup.GetActiveOffersAsync(programIds, cancellationToken);
        var accessibleProgramIds = query.UserId is null
            ? null
            : await programAccessContext.GetAccessibleProgramIdsAsync(query.UserId.Value, programIds, cancellationToken);

        var items = new List<ClientProgramSummaryDto>(programs.Count);
        foreach (var program in programs)
        {
            var resolution = TranslationResolver.Resolve(
                translations.Where(t => t.ProgramId == program.Id),
                query.RequestedLanguage,
                program.DefaultLanguage);

            var activeOffer = activeOffers.GetValueOrDefault(program.Id);
            var ownershipState = accessibleProgramIds is null
                ? null
                : accessibleProgramIds.Contains(program.Id)
                    ? ClientProgramOwnershipState.Owned
                    : ClientProgramOwnershipState.NotOwned;

            items.Add(new ClientProgramSummaryDto(
                program.Id,
                program.Slug,
                program.DomainId,
                resolution.Translation.Title,
                resolution.Translation.ShortDescription,
                program.CoverAssetId,
                program.SortOrder,
                activeOffer is null ? null : new ClientProgramOfferDto(activeOffer.Amount, activeOffer.Currency),
                ownershipState));
        }

        return items;
    }
}
