using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed class GetProgramOfferDetailHandler(DbContext dbContext)
{
    public async Task<ProgramOfferDetailDto> HandleAsync(Guid programOfferId, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Set<ProgramOffer>().AsNoTracking().SingleOrDefaultAsync(o => o.Id == programOfferId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program offer does not exist.");

        var priceHistory = await dbContext.Set<ProgramPrice>().AsNoTracking()
            .Where(p => p.ProgramOfferId == programOfferId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new ProgramPriceDto(p.Id, p.Amount, p.Currency, p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var purchaseCount = await dbContext.Set<Purchase>().AsNoTracking()
            .CountAsync(p => p.ProgramOfferId == programOfferId, cancellationToken);

        var succeededPurchaseCount = await dbContext.Set<Purchase>().AsNoTracking()
            .CountAsync(p => p.ProgramOfferId == programOfferId && p.Status == PurchaseStatus.Succeeded, cancellationToken);

        return new ProgramOfferDetailDto(
            offer.Id,
            offer.ProgramId,
            offer.Status.ToString(),
            offer.CreatedAt,
            offer.UpdatedAt,
            priceHistory,
            purchaseCount,
            succeededPurchaseCount);
    }
}
