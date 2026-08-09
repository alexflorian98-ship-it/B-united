using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed class ListProgramOffersHandler(DbContext dbContext)
{
    public async Task<ProgramOfferListResult> HandleAsync(ListProgramOffersQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<ProgramOffer>().AsNoTracking().AsQueryable();
        if (query.Status is not null)
        {
            baseQuery = baseQuery.Where(o => o.Status == query.Status);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var offerIds = page.Select(o => o.Id).ToList();

        // Fetched and grouped client-side (not translated as a per-offer correlated subquery):
        // per page (<= PageSize offers), this is a small, bounded read.
        var allPrices = await dbContext.Set<ProgramPrice>().AsNoTracking()
            .Where(p => offerIds.Contains(p.ProgramOfferId))
            .ToListAsync(cancellationToken);
        var latestPriceByOffer = allPrices
            .GroupBy(p => p.ProgramOfferId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreatedAtUtc).First());

        var items = page.Select(offer =>
        {
            latestPriceByOffer.TryGetValue(offer.Id, out var latestPrice);
            return new ProgramOfferSummaryDto(
                offer.Id,
                offer.ProgramId,
                offer.Status.ToString(),
                latestPrice?.Amount,
                latestPrice?.Currency,
                offer.CreatedAt,
                offer.UpdatedAt);
        }).ToList();

        return new ProgramOfferListResult(items, totalCount);
    }
}
