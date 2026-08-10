using BUnited.Modules.Billing.Application.UseCases.Admin;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Tests.TestSupport;
using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Billing.Tests.Application.Admin;

/// <summary>P3.20.b: proves the admin purchases list's server-side filter/sort actually filters
/// and sorts, since the frontend controls only ever forward parameters the backend genuinely
/// supports (CLAUDE.md: never invent a UI capability the server can't perform).</summary>
public sealed class ListPurchasesHandlerTests
{
    private sealed class NullUserLookup : IUserLookup
    {
        public Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<UserSummary?>(null);

        public Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, UserSummary>>(new Dictionary<Guid, UserSummary>());
    }

    [Fact]
    public async Task Filters_by_status()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var succeeded = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 99m, "RON");
        succeeded.MarkSucceeded(DateTime.UtcNow);
        var pending = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 50m, "RON");
        dbContext.AddRange(offer, price, succeeded, pending);
        await dbContext.SaveChangesAsync();

        var result = await new ListPurchasesHandler(dbContext, new NullUserLookup())
            .HandleAsync(new ListPurchasesQuery(1, 25, Status: PurchaseStatus.Succeeded), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(succeeded.Id, item.PurchaseId);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Filters_by_program_id()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offerA = ProgramOffer.Create(Guid.NewGuid());
        var priceA = ProgramPrice.Create(offerA.Id, 99m, "RON", DateTime.UtcNow);
        var offerB = ProgramOffer.Create(Guid.NewGuid());
        var priceB = ProgramPrice.Create(offerB.Id, 40m, "RON", DateTime.UtcNow);
        var purchaseA = Purchase.Create(Guid.NewGuid(), offerA.ProgramId, offerA.Id, priceA.Id, 99m, "RON");
        var purchaseB = Purchase.Create(Guid.NewGuid(), offerB.ProgramId, offerB.Id, priceB.Id, 40m, "RON");
        dbContext.AddRange(offerA, priceA, offerB, priceB, purchaseA, purchaseB);
        await dbContext.SaveChangesAsync();

        var result = await new ListPurchasesHandler(dbContext, new NullUserLookup())
            .HandleAsync(new ListPurchasesQuery(1, 25, ProgramId: offerB.ProgramId), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(purchaseB.Id, item.PurchaseId);
    }

    [Fact]
    public async Task Sorts_by_amount_ascending()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        var high = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 200m, "RON");
        var low = Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 10m, "RON");
        dbContext.AddRange(offer, price, high, low);
        await dbContext.SaveChangesAsync();

        var result = await new ListPurchasesHandler(dbContext, new NullUserLookup())
            .HandleAsync(new ListPurchasesQuery(1, 25, SortBy: PurchaseSortBy.Amount, Descending: false), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(low.Id, result.Items[0].PurchaseId);
        Assert.Equal(high.Id, result.Items[1].PurchaseId);
    }

    [Fact]
    public async Task Applies_pagination()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        var price = ProgramPrice.Create(offer.Id, 99m, "RON", DateTime.UtcNow);
        for (var i = 0; i < 3; i++)
        {
            dbContext.Add(Purchase.Create(Guid.NewGuid(), offer.ProgramId, offer.Id, price.Id, 10m + i, "RON"));
        }

        dbContext.AddRange(offer, price);
        await dbContext.SaveChangesAsync();

        var result = await new ListPurchasesHandler(dbContext, new NullUserLookup())
            .HandleAsync(new ListPurchasesQuery(2, 2), CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
    }
}
