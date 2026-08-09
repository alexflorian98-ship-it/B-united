using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.UseCases.Admin;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Tests.TestSupport;
using BUnited.Modules.Content.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Tests.Application;

/// <summary>P3.35 — admin program-offer management: creation validated against Content via
/// <see cref="FakeProgramLookup"/> (a stand-in for the real cross-module <c>IProgramLookup</c>,
/// CLAUDE.md), price history is append-only, and activate/deactivate are guarded against a
/// second concurrently active offer per program.</summary>
public sealed class AdminProgramOfferManagementTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    private sealed record Fixture(
        TestDbContext DbContext,
        FakeAuditLogger AuditLogger,
        FakeProgramLookup ProgramLookup,
        CreateProgramOfferHandler CreateHandler,
        UpdateProgramOfferPriceHandler UpdatePriceHandler,
        ProgramOfferStatusHandler StatusHandler,
        ListProgramOffersHandler ListHandler,
        GetProgramOfferDetailHandler DetailHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var auditLogger = new FakeAuditLogger();
        var programLookup = new FakeProgramLookup();
        return new Fixture(
            context,
            auditLogger,
            programLookup,
            new CreateProgramOfferHandler(context, programLookup, auditLogger),
            new UpdateProgramOfferPriceHandler(context, auditLogger),
            new ProgramOfferStatusHandler(context, auditLogger),
            new ListProgramOffersHandler(context),
            new GetProgramOfferDetailHandler(context));
    }

    [Fact]
    public async Task Creating_an_offer_for_a_published_program_succeeds_and_writes_an_audit_entry()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);

        var result = await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", false, ActorId), CancellationToken.None);

        var offer = fx.DbContext.ProgramOffers.Single(o => o.Id == result.ProgramOfferId);
        Assert.Equal(ProgramOfferStatus.Draft, offer.Status);
        var price = fx.DbContext.ProgramPrices.Single(p => p.Id == result.ProgramPriceId);
        Assert.Equal(199.00m, price.Amount);
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "program_offer.created");
    }

    [Fact]
    public async Task Creating_an_offer_with_activate_immediately_activates_it()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);

        var result = await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", true, ActorId), CancellationToken.None);

        var offer = fx.DbContext.ProgramOffers.Single(o => o.Id == result.ProgramOfferId);
        Assert.Equal(ProgramOfferStatus.Active, offer.Status);
    }

    [Fact]
    public async Task Creating_an_offer_for_a_nonexistent_program_is_not_found()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(Guid.NewGuid(), 199.00m, "RON", false, ActorId), CancellationToken.None));
    }

    [Fact]
    public async Task Creating_an_offer_for_an_unpublished_program_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId, ProgramLookupStatus.Draft);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", false, ActorId), CancellationToken.None));

        Assert.Equal("PROGRAM_OFFER_PROGRAM_NOT_PUBLISHED", ex.Code);
    }

    [Fact]
    public async Task Creating_a_second_offer_while_one_is_already_active_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", true, ActorId), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 249.00m, "RON", false, ActorId), CancellationToken.None));

        Assert.Equal("PROGRAM_OFFER_ALREADY_ACTIVE", ex.Code);
    }

    [Fact]
    public async Task Updating_the_price_appends_a_new_row_and_never_mutates_the_previous_one()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        var created = await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", false, ActorId), CancellationToken.None);

        var newPriceId = await fx.UpdatePriceHandler.HandleAsync(
            new UpdateProgramOfferPriceCommand(created.ProgramOfferId, 249.00m, "RON", ActorId), CancellationToken.None);

        Assert.NotEqual(created.ProgramPriceId, newPriceId);
        var originalPrice = fx.DbContext.ProgramPrices.Single(p => p.Id == created.ProgramPriceId);
        Assert.Equal(199.00m, originalPrice.Amount);
        var newPrice = fx.DbContext.ProgramPrices.Single(p => p.Id == newPriceId);
        Assert.Equal(249.00m, newPrice.Amount);
        Assert.Equal(2, fx.DbContext.ProgramPrices.Count(p => p.ProgramOfferId == created.ProgramOfferId));
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "program_offer.price_changed");
    }

    [Fact]
    public async Task Activating_an_offer_with_no_price_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var offer = ProgramOffer.Create(Guid.NewGuid());
        fx.DbContext.ProgramOffers.Add(offer);
        await fx.DbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.StatusHandler.ActivateAsync(offer.Id, ActorId, CancellationToken.None));

        Assert.Equal("PROGRAM_OFFER_NO_PRICE", ex.Code);
    }

    [Fact]
    public async Task Activating_an_offer_while_another_is_already_active_for_the_same_program_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", true, ActorId), CancellationToken.None);
        var secondOffer = ProgramOffer.Create(programId);
        fx.DbContext.ProgramOffers.Add(secondOffer);
        fx.DbContext.ProgramPrices.Add(ProgramPrice.Create(secondOffer.Id, 149.00m, "RON", DateTime.UtcNow));
        await fx.DbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.StatusHandler.ActivateAsync(secondOffer.Id, ActorId, CancellationToken.None));

        Assert.Equal("PROGRAM_OFFER_ALREADY_ACTIVE", ex.Code);
    }

    [Fact]
    public async Task Deactivating_an_active_offer_flips_its_status_and_writes_an_audit_entry()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        var created = await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", true, ActorId), CancellationToken.None);

        await fx.StatusHandler.DeactivateAsync(created.ProgramOfferId, ActorId, CancellationToken.None);

        var offer = fx.DbContext.ProgramOffers.Single(o => o.Id == created.ProgramOfferId);
        Assert.Equal(ProgramOfferStatus.Inactive, offer.Status);
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "program_offer.deactivated");
    }

    [Fact]
    public async Task Activating_a_nonexistent_offer_is_not_found()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            fx.StatusHandler.ActivateAsync(Guid.NewGuid(), ActorId, CancellationToken.None));
    }

    [Fact]
    public async Task List_offers_returns_the_latest_price_and_supports_a_status_filter()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId1 = Guid.NewGuid();
        var programId2 = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId1);
        fx.ProgramLookup.AddProgram(programId2);
        var offer1 = await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId1, 199.00m, "RON", true, ActorId), CancellationToken.None);
        await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId2, 99.00m, "RON", false, ActorId), CancellationToken.None);
        await fx.UpdatePriceHandler.HandleAsync(new UpdateProgramOfferPriceCommand(offer1.ProgramOfferId, 249.00m, "RON", ActorId), CancellationToken.None);

        var all = await fx.ListHandler.HandleAsync(new ListProgramOffersQuery(null, 1, 25), CancellationToken.None);
        Assert.Equal(2, all.TotalCount);
        var offer1Summary = all.Items.Single(o => o.Id == offer1.ProgramOfferId);
        Assert.Equal(249.00m, offer1Summary.CurrentAmount);

        var activeOnly = await fx.ListHandler.HandleAsync(new ListProgramOffersQuery(ProgramOfferStatus.Active, 1, 25), CancellationToken.None);
        Assert.Single(activeOnly.Items);
        Assert.Equal(offer1.ProgramOfferId, activeOnly.Items.Single().Id);
    }

    [Fact]
    public async Task Offer_detail_reports_price_history_and_purchase_counts()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        var created = await fx.CreateHandler.HandleAsync(new CreateProgramOfferCommand(programId, 199.00m, "RON", true, ActorId), CancellationToken.None);
        await fx.UpdatePriceHandler.HandleAsync(new UpdateProgramOfferPriceCommand(created.ProgramOfferId, 249.00m, "RON", ActorId), CancellationToken.None);

        var purchase = Purchase.Create(Guid.NewGuid(), programId, created.ProgramOfferId, created.ProgramPriceId, 199.00m, "RON");
        purchase.MarkSucceeded(DateTime.UtcNow);
        fx.DbContext.Purchases.Add(purchase);
        await fx.DbContext.SaveChangesAsync();

        var detail = await fx.DetailHandler.HandleAsync(created.ProgramOfferId, CancellationToken.None);

        Assert.Equal(2, detail.PriceHistory.Count);
        Assert.Equal(1, detail.PurchaseCount);
        Assert.Equal(1, detail.SucceededPurchaseCount);
    }

    [Fact]
    public async Task Getting_a_nonexistent_offer_detail_is_not_found()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            fx.DetailHandler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
