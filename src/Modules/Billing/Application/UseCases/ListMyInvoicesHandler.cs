using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class ListMyInvoicesHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<MyInvoiceDto>> HandleAsync(Guid userId, CancellationToken cancellationToken) =>
        await (from invoice in dbContext.Set<Invoice>().AsNoTracking()
               join purchase in dbContext.Set<Purchase>().AsNoTracking() on invoice.PurchaseId equals purchase.Id
               where purchase.UserId == userId
               orderby invoice.IssuedAtUtc descending
               select new MyInvoiceDto(invoice.Id, invoice.PurchaseId, purchase.ProgramId, purchase.ProgramTitleSnapshot, invoice.Amount, invoice.Currency, invoice.Status.ToString(), invoice.IssuedAtUtc))
            .ToListAsync(cancellationToken);
}
