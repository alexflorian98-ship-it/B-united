using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>§54 purchases table. Resolves client emails via <see cref="IUserLookup"/> — a
/// read-only cross-module admin projection (CLAUDE.md), never a reference to Identity's Domain.</summary>
public sealed class ListPurchasesHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<PurchaseListResult> HandleAsync(ListPurchasesQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<Purchase>().AsNoTracking().OrderByDescending(p => p.CreatedAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = page.Select(p => p.UserId).Distinct().ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        var items = page.Select(purchase => new PurchaseSummaryDto(
            purchase.Id,
            purchase.UserId,
            users.TryGetValue(purchase.UserId, out var user) ? user.Email : null,
            purchase.ProgramId,
            purchase.ProgramTitleSnapshot,
            purchase.Amount,
            purchase.Currency,
            purchase.Status.ToString(),
            purchase.CreatedAt,
            purchase.CompletedAtUtc)).ToList();

        return new PurchaseListResult(items, totalCount);
    }
}
