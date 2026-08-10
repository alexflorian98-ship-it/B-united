using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>P3.19.b: single-invoice detail/receipt view. Ownership is enforced here — an invoice
/// is only ever resolved through its owning purchase's <c>UserId</c>, matching the same
/// never-confirm-another-user's-resource-exists pattern used across the module (a mismatch or
/// missing invoice both surface as <see cref="NotFoundAppException"/>, never a 403).</summary>
public sealed class GetMyInvoiceHandler(DbContext dbContext)
{
    public async Task<MyInvoiceDto> HandleAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await (from invoice in dbContext.Set<Invoice>().AsNoTracking()
                             join purchase in dbContext.Set<Purchase>().AsNoTracking() on invoice.PurchaseId equals purchase.Id
                             where invoice.Id == invoiceId && purchase.UserId == userId
                             select new MyInvoiceDto(invoice.Id, invoice.PurchaseId, purchase.ProgramId, purchase.ProgramTitleSnapshot, invoice.Amount, invoice.Currency, invoice.Status.ToString(), invoice.IssuedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? throw new NotFoundAppException("The specified invoice does not exist.");
    }
}
