using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Tests.TestSupport;

namespace BUnited.Modules.Billing.Tests.Application;

public sealed class GetMyInvoiceHandlerTests
{
    [Fact]
    public async Task Returns_the_invoice_when_owned_by_the_caller()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var purchase = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        var invoice = Invoice.Create(purchase.Id, null, 99m, "RON", InvoiceStatus.Paid, DateTime.UtcNow);
        dbContext.AddRange(offer, price, purchase, invoice);
        await dbContext.SaveChangesAsync();

        var result = await new GetMyInvoiceHandler(dbContext).HandleAsync(purchase.UserId, invoice.Id, CancellationToken.None);

        Assert.Equal(invoice.Id, result.Id);
        Assert.Equal(purchase.ProgramId, result.ProgramId);
        Assert.Equal(99m, result.Amount);
    }

    [Fact]
    public async Task Throws_not_found_when_the_invoice_belongs_to_another_user()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var purchase = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        var invoice = Invoice.Create(purchase.Id, null, 99m, "RON", InvoiceStatus.Paid, DateTime.UtcNow);
        dbContext.AddRange(offer, price, purchase, invoice);
        await dbContext.SaveChangesAsync();

        var otherUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            new GetMyInvoiceHandler(dbContext).HandleAsync(otherUserId, invoice.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_not_found_when_the_invoice_does_not_exist()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            new GetMyInvoiceHandler(dbContext).HandleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }
}
