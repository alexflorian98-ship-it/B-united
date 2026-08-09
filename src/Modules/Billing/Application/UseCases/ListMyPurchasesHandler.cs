using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class ListMyPurchasesHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<PurchaseDto>> HandleAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<Purchase>().AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PurchaseDto(p.Id, p.ProgramId, p.ProgramTitleSnapshot, p.Amount, p.Currency, p.Status.ToString(), p.CreatedAt, p.CompletedAtUtc))
            .ToListAsync(cancellationToken);
}
