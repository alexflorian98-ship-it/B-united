using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Tests.TestSupport;

namespace BUnited.Modules.Billing.Tests.Application;

public sealed class ListMyInvoicesHandlerTests
{
    [Fact]
    public async Task Returns_only_invoices_for_the_requested_user()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var ownerPurchase = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        var otherPurchase = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        dbContext.AddRange(offer, price, ownerPurchase, otherPurchase);
        dbContext.AddRange(
            Invoice.Create(ownerPurchase.Id, null, 99m, "RON", InvoiceStatus.Paid, DateTime.UtcNow),
            Invoice.Create(otherPurchase.Id, null, 99m, "RON", InvoiceStatus.Paid, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var result = await new ListMyInvoicesHandler(dbContext).HandleAsync(ownerPurchase.UserId, CancellationToken.None);

        var invoice = Assert.Single(result);
        Assert.Equal(ownerPurchase.Id, invoice.PurchaseId);
        Assert.Equal(ownerPurchase.ProgramId, invoice.ProgramId);
    }
}
