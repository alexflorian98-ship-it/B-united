using BUnited.Modules.Billing.Contracts;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Infrastructure.CrossModule;

public sealed class ProgramOfferLookup(DbContext dbContext) : IProgramOfferLookup
{
    public async Task<ActiveOfferSummary?> GetActiveOfferAsync(Guid programId, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Set<ProgramOffer>().AsNoTracking()
            .SingleOrDefaultAsync(o => o.ProgramId == programId && o.Status == ProgramOfferStatus.Active, cancellationToken);

        if (offer is null)
        {
            return null;
        }

        var price = await dbContext.Set<ProgramPrice>().AsNoTracking()
            .Where(p => p.ProgramOfferId == offer.Id)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (price is null)
        {
            return null;
        }

        return new ActiveOfferSummary(offer.Id, price.Id, price.Amount, price.Currency);
    }

    public async Task<IReadOnlyDictionary<Guid, ActiveOfferSummary>> GetActiveOffersAsync(
        IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken)
    {
        if (programIds.Count == 0)
        {
            return new Dictionary<Guid, ActiveOfferSummary>();
        }

        var offers = await dbContext.Set<ProgramOffer>().AsNoTracking()
            .Where(o => programIds.Contains(o.ProgramId) && o.Status == ProgramOfferStatus.Active)
            .ToListAsync(cancellationToken);

        var offerIds = offers.Select(o => o.Id).ToList();
        var prices = await dbContext.Set<ProgramPrice>().AsNoTracking()
            .Where(p => offerIds.Contains(p.ProgramOfferId))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, ActiveOfferSummary>();
        foreach (var offer in offers)
        {
            var price = prices.Where(p => p.ProgramOfferId == offer.Id)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefault();
            if (price is not null)
            {
                result[offer.ProgramId] = new ActiveOfferSummary(offer.Id, price.Id, price.Amount, price.Currency);
            }
        }

        return result;
    }
}
