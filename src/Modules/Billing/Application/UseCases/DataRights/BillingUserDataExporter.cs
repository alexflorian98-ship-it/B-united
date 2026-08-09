using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.DataRights;

/// <summary>Billing's section of the full-account export archive — the caller's own purchases,
/// payments, invoices, and entitlements, reusing the existing "my"-scoped handlers where they
/// already exist. There is deliberately no matching <c>IUserDataEraser</c>: Billing records are
/// retained indefinitely on account deletion per docs/DATA_RETENTION_POLICY.md (financial/audit
/// record-keeping, §66).</summary>
public sealed class BillingUserDataExporter(
    ListMyPurchasesHandler listMyPurchasesHandler,
    ListMyInvoicesHandler listMyInvoicesHandler,
    ListMyEntitlementsHandler listMyEntitlementsHandler,
    DbContext dbContext) : IUserDataExporter
{
    public string SectionKey => "billing";

    public async Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var purchases = await listMyPurchasesHandler.HandleAsync(userId, cancellationToken);
        var invoices = await listMyInvoicesHandler.HandleAsync(userId, cancellationToken);
        var entitlements = await listMyEntitlementsHandler.HandleAsync(userId, cancellationToken);

        var payments = await (
            from payment in dbContext.Set<Payment>().AsNoTracking()
            join purchase in dbContext.Set<Purchase>().AsNoTracking() on payment.PurchaseId equals purchase.Id
            where purchase.UserId == userId
            orderby payment.OccurredAtUtc descending
            select new
            {
                payment.Id,
                payment.PurchaseId,
                payment.Amount,
                payment.Currency,
                Status = payment.Status.ToString(),
                payment.OccurredAtUtc,
            }).ToListAsync(cancellationToken);

        return new { Purchases = purchases, Payments = payments, Invoices = invoices, Entitlements = entitlements };
    }
}
