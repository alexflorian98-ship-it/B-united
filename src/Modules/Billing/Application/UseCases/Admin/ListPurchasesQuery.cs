using BUnited.Modules.Billing.Domain;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>P3.20.b: <paramref name="Status"/>/<paramref name="ProgramId"/> filter and
/// <paramref name="SortBy"/>/<paramref name="Descending"/> sort are all applied server-side —
/// scoped to columns the §54 purchases table already renders, so the admin UI never claims a
/// capability the backend can't actually perform.</summary>
public sealed record ListPurchasesQuery(
    int Page,
    int PageSize,
    PurchaseStatus? Status = null,
    Guid? ProgramId = null,
    PurchaseSortBy SortBy = PurchaseSortBy.CreatedAt,
    bool Descending = true);

public enum PurchaseSortBy
{
    CreatedAt,
    Amount,
}
