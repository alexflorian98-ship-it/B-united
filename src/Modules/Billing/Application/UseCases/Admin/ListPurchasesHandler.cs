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
        var filtered = dbContext.Set<Purchase>().AsNoTracking().AsQueryable();

        if (query.Status is not null)
        {
            filtered = filtered.Where(p => p.Status == query.Status);
        }

        if (query.ProgramId is not null)
        {
            filtered = filtered.Where(p => p.ProgramId == query.ProgramId);
        }

        // Sort key is cast to double rather than compared as decimal: PostgreSQL (production)
        // handles ORDER BY on decimal natively, but EF Core's SQLite provider (used by the
        // in-memory test suite) cannot translate it — casting keeps one query shape valid on both
        // providers without weakening the persisted decimal amount used for display/money math.
        var sorted = (query.SortBy, query.Descending) switch
        {
            (PurchaseSortBy.Amount, true) => filtered.OrderByDescending(p => (double)p.Amount),
            (PurchaseSortBy.Amount, false) => filtered.OrderBy(p => (double)p.Amount),
            (PurchaseSortBy.CreatedAt, false) => filtered.OrderBy(p => p.CreatedAt),
            _ => filtered.OrderByDescending(p => p.CreatedAt),
        };

        var totalCount = await filtered.CountAsync(cancellationToken);

        var page = await sorted
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
