using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Contracts;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Infrastructure.Access;
using BUnited.Modules.Billing.Infrastructure.CrossModule;
using BUnited.Modules.Billing.Infrastructure.Payments;
using BUnited.Modules.Billing.Tests.TestSupport;

namespace BUnited.Modules.Billing.Tests.Application;

public sealed class ProgramCommerceFlowTests
{
    private sealed record Fixture(
        TestDbContext DbContext,
        FakeAuditLogger AuditLogger,
        IPaymentProvider PaymentProvider,
        IProgramAccessContext AccessContext,
        IProgramOfferLookup OfferLookup,
        FakeProgramLookup ProgramLookup,
        ProcessProviderEventHandler EventHandler,
        CreateProgramPurchaseHandler PurchaseHandler,
        TriggerDemoEventHandler DemoEventHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var auditLogger = new FakeAuditLogger();
        var paymentProvider = new FakePaymentProvider();
        var accessContext = new BillingProgramAccessContext(context);
        var offerLookup = new ProgramOfferLookup(context);
        var programLookup = new FakeProgramLookup();
        var eventHandler = new ProcessProviderEventHandler(context, auditLogger);
        var purchaseHandler = new CreateProgramPurchaseHandler(context, paymentProvider, accessContext, offerLookup, programLookup, eventHandler, auditLogger);
        var demoEventHandler = new TriggerDemoEventHandler(context, paymentProvider, eventHandler);
        return new Fixture(context, auditLogger, paymentProvider, accessContext, offerLookup, programLookup, eventHandler, purchaseHandler, demoEventHandler);
    }

    private static (Guid ProgramId, Guid OfferId, Guid PriceId) SeedActiveOffer(TestDbContext dbContext, decimal amount = 99.00m, string currency = "RON")
    {
        var programId = Guid.NewGuid();
        var offer = ProgramOffer.Create(programId);
        offer.Activate();
        dbContext.ProgramOffers.Add(offer);
        var price = ProgramPrice.Create(offer.Id, amount, currency, DateTime.UtcNow);
        dbContext.ProgramPrices.Add(price);
        dbContext.SaveChanges();
        return (programId, offer.Id, price.Id);
    }

    [Fact]
    public async Task Successful_checkout_grants_program_entitlement()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        Assert.Equal(PurchaseStatus.Succeeded.ToString(), result.Status);

        var entitlement = fx.DbContext.ProgramEntitlements.Single(e => e.UserId == userId && e.ProgramId == programId);
        Assert.True(entitlement.IsActive);
        Assert.True(await fx.AccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "payment.webhook_processed");
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "purchase.succeeded");
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "program_access.granted");
    }

    /// <summary>Security-gap-closure item #1 (two-user IDOR suite, "program entitlements") and
    /// item #3 ("entitlement is scoped to user ID and program ID"): exercises the REAL
    /// <see cref="BillingProgramAccessContext"/> — not the <c>FakeProgramAccessContext</c> every
    /// other module's tests use — with two distinct users who each purchase a DIFFERENT program,
    /// proving entitlement is a (UserId, ProgramId) pair, not just a per-user flag or a
    /// per-program flag. Either dimension being wrong would let one client watch another
    /// client's paid content.</summary>
    [Fact]
    public async Task Entitlement_is_scoped_to_both_user_and_program_not_either_alone()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programA, _, _) = SeedActiveOffer(fx.DbContext, amount: 99.00m);
        var (programB, _, _) = SeedActiveOffer(fx.DbContext, amount: 149.00m);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userA, programA, CheckoutOutcome.Success), CancellationToken.None);
        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userB, programB, CheckoutOutcome.Success), CancellationToken.None);

        Assert.True(await fx.AccessContext.HasProgramAccessAsync(userA, programA, CancellationToken.None));
        Assert.True(await fx.AccessContext.HasProgramAccessAsync(userB, programB, CancellationToken.None));

        // Neither dimension alone is sufficient: right user/wrong program, and right
        // program/wrong user, must both be denied.
        Assert.False(await fx.AccessContext.HasProgramAccessAsync(userA, programB, CancellationToken.None));
        Assert.False(await fx.AccessContext.HasProgramAccessAsync(userB, programA, CancellationToken.None));

        var accessibleForUserA = await fx.AccessContext.GetAccessibleProgramIdsAsync(userA, [programA, programB], CancellationToken.None);
        Assert.Equal(new HashSet<Guid> { programA }, accessibleForUserA);
    }

    [Fact]
    public async Task Checkout_snapshots_the_programs_current_title_onto_the_purchase()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        fx.ProgramLookup.AddProgram(programId, slug: "resilience-101", title: "Resilience 101");
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        var purchase = fx.DbContext.Purchases.Single(p => p.Id == result.PurchaseId);
        Assert.Equal("Resilience 101", purchase.ProgramTitleSnapshot);
    }

    [Fact]
    public async Task Purchase_title_snapshot_is_unaffected_by_a_later_lookup_change_simulating_a_program_rename()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        fx.ProgramLookup.AddProgram(programId, slug: "resilience-101", title: "Resilience 101");
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        // Simulate the program being renamed (or unpublished/archived) after the purchase.
        fx.ProgramLookup.AddProgram(programId, slug: "resilience-101", title: "Resilience 201 (renamed)");

        var purchase = fx.DbContext.Purchases.Single(p => p.Id == result.PurchaseId);
        Assert.Equal("Resilience 101", purchase.ProgramTitleSnapshot);
    }

    [Fact]
    public async Task Declined_checkout_does_not_grant_access()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Decline), CancellationToken.None);

        Assert.Equal(PurchaseStatus.Failed.ToString(), result.Status);
        Assert.False(fx.DbContext.ProgramEntitlements.Any(e => e.UserId == userId));
    }

    [Fact]
    public async Task Provider_error_checkout_produces_no_event_and_leaves_purchase_pending()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.ProviderError), CancellationToken.None);

        Assert.Equal(PurchaseStatus.Pending.ToString(), result.Status);
        Assert.Empty(fx.DbContext.WebhookEvents);
    }

    [Fact]
    public async Task Timeout_checkout_produces_no_event_and_leaves_purchase_pending()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Timeout), CancellationToken.None);

        Assert.Equal(PurchaseStatus.Pending.ToString(), result.Status);
        Assert.Empty(fx.DbContext.WebhookEvents);
    }

    [Fact]
    public async Task Duplicate_event_delivery_grants_a_single_entitlement()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, offerId, priceId) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();
        var purchase = Purchase.Create(userId, programId, offerId, priceId, 99.00m, "RON");
        fx.DbContext.Purchases.Add(purchase);
        await fx.DbContext.SaveChangesAsync();

        var evt = fx.PaymentProvider.CreateDemoEvent(purchase.Id, ProviderEventType.PaymentSucceeded, 99.00m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(evt, CancellationToken.None);
        await fx.EventHandler.HandleAsync(evt, CancellationToken.None);

        Assert.Single(fx.DbContext.WebhookEvents);
        Assert.Single(fx.DbContext.ProgramEntitlements.Where(e => e.UserId == userId && e.ProgramId == programId));
    }

    /// <summary>P3.23.b. Unlike <see cref="Duplicate_event_delivery_grants_a_single_entitlement"/>
    /// (sequential — the second call's "does a row already exist" pre-check sees the first call's
    /// completed commit and takes the clean early-return path), this fires two truly concurrent
    /// calls, each on its own thread and its own <see cref="DbContext"/>/connection, for the SAME
    /// provider event. Both can pass the pre-check before either commits, so the loser is expected
    /// to hit the real unique-index violation on <c>WebhookEvent.ProviderEventId</c> as a
    /// <see cref="DbUpdateException"/> — <see cref="ProcessProviderEventHandler"/> must catch that
    /// and recover as an idempotent no-op rather than let it surface to the caller (which, for a
    /// live webhook endpoint, would mean reporting a delivery failure back to the payment provider
    /// for an event that in fact was fully processed).</summary>
    [Fact]
    public async Task Concurrent_duplicate_event_delivery_processes_exactly_once()
    {
        var (connection1, context1, connection2, context2) = TestDbContextFactory.CreateConcurrentPair();
        using var _1 = connection1;
        using var _2 = connection2;
        using var __1 = context1;
        using var __2 = context2;

        var (programId, offerId, priceId) = SeedActiveOffer(context1);
        var userId = Guid.NewGuid();
        var purchase = Purchase.Create(userId, programId, offerId, priceId, 99.00m, "RON");
        context1.Purchases.Add(purchase);
        await context1.SaveChangesAsync();

        var paymentProvider = new FakePaymentProvider();
        var evt = paymentProvider.CreateDemoEvent(purchase.Id, ProviderEventType.PaymentSucceeded, 99.00m, "RON", DateTime.UtcNow);

        var handler1 = new ProcessProviderEventHandler(context1, new FakeAuditLogger());
        var handler2 = new ProcessProviderEventHandler(context2, new FakeAuditLogger());

        // Both requests race the SAME provider event on separate threads — neither call is
        // allowed to surface an unhandled exception to its caller.
        var task1 = Task.Run(() => handler1.HandleAsync(evt, CancellationToken.None));
        var task2 = Task.Run(() => handler2.HandleAsync(evt, CancellationToken.None));
        await Task.WhenAll(task1, task2);

        Assert.Single(context1.WebhookEvents.Where(e => e.ProviderEventId == evt.ProviderEventId));
        Assert.Single(context1.ProgramEntitlements.Where(e => e.UserId == userId && e.ProgramId == programId));
        Assert.Single(context1.Payments.Where(p => p.PurchaseId == purchase.Id));
        Assert.Single(context1.Invoices.Where(i => i.PurchaseId == purchase.Id));
        Assert.True(context1.ProgramEntitlements.Single(e => e.UserId == userId && e.ProgramId == programId).IsActive);
    }

    [Fact]
    public async Task Out_of_order_event_does_not_regress_state()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, offerId, priceId) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();
        var purchase = Purchase.Create(userId, programId, offerId, priceId, 99.00m, "RON");
        fx.DbContext.Purchases.Add(purchase);
        await fx.DbContext.SaveChangesAsync();

        var utcNow = DateTime.UtcNow;
        var laterSucceeded = new ProviderEvent("evt-later", ProviderEventType.PaymentSucceeded, purchase.Id, 99m, "RON", utcNow, "{}");
        var earlierFailed = new ProviderEvent("evt-earlier", ProviderEventType.PaymentFailed, purchase.Id, 99m, "RON", utcNow.AddMinutes(-5), "{}");

        await fx.EventHandler.HandleAsync(laterSucceeded, CancellationToken.None);
        await fx.EventHandler.HandleAsync(earlierFailed, CancellationToken.None);

        var refreshed = fx.DbContext.Purchases.Single(p => p.Id == purchase.Id);
        // The earlier (stale) failure must not regress the already-Succeeded purchase.
        Assert.Equal(PurchaseStatus.Succeeded, refreshed.Status);
        Assert.Equal(2, fx.DbContext.WebhookEvents.Count());
        Assert.True(fx.DbContext.ProgramEntitlements.Single(e => e.UserId == userId).IsActive);
    }

    [Fact]
    public async Task Duplicate_checkout_reuses_the_pending_purchase()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var first = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.ProviderError), CancellationToken.None);
        var second = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.ProviderError), CancellationToken.None);

        Assert.Equal(first.PurchaseId, second.PurchaseId);
        Assert.Single(fx.DbContext.Purchases.Where(p => p.UserId == userId && p.ProgramId == programId));
    }

    /// <summary>P3.31.b. A client whose first checkout attempt hit a transient provider outcome
    /// (Timeout — modeled identically to ProviderError per P3.31.a) retries the exact same
    /// purchase. Documents the actual current behavior: the retry reuses the still-Pending
    /// purchase created by the first attempt (same guard as
    /// <see cref="Duplicate_checkout_reuses_the_pending_purchase"/>) and, once it resolves
    /// successfully, grants exactly one entitlement with no duplicate <see cref="Purchase"/> or
    /// <see cref="ProgramEntitlement"/> row — a clean retry succeeds, it is not rejected as
    /// "already exists".</summary>
    [Fact]
    public async Task Retry_after_transient_provider_failure_succeeds_without_duplicating_purchase_or_entitlement()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var first = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Timeout), CancellationToken.None);
        Assert.Equal(PurchaseStatus.Pending.ToString(), first.Status);

        var retry = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        Assert.Equal(first.PurchaseId, retry.PurchaseId);
        Assert.Equal(PurchaseStatus.Succeeded.ToString(), retry.Status);
        Assert.Single(fx.DbContext.Purchases.Where(p => p.UserId == userId && p.ProgramId == programId));
        Assert.Single(fx.DbContext.ProgramEntitlements.Where(e => e.UserId == userId && e.ProgramId == programId));
        Assert.True(await fx.AccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));

        // A second retry after the purchase already succeeded is rejected up front by the
        // already-owned guard — it must not create a second purchase or a second entitlement.
        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None));
        Assert.Equal(ProgramAccessErrorCodes.ProgramAlreadyOwned, ex.Code);
        Assert.Single(fx.DbContext.Purchases.Where(p => p.UserId == userId && p.ProgramId == programId));
        Assert.Single(fx.DbContext.ProgramEntitlements.Where(e => e.UserId == userId && e.ProgramId == programId));
    }

    [Fact]
    public async Task Checkout_on_an_already_owned_program_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None));

        Assert.Equal(ProgramAccessErrorCodes.ProgramAlreadyOwned, ex.Code);
    }

    [Fact]
    public async Task Refund_flips_status_and_revokes_access_without_deleting_history()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);
        await fx.DemoEventHandler.HandleAsync(new TriggerDemoEventCommand(userId, programId, DemoAction.Refund), CancellationToken.None);

        var purchase = fx.DbContext.Purchases.Single(p => p.Id == result.PurchaseId);
        var entitlement = fx.DbContext.ProgramEntitlements.Single(e => e.UserId == userId && e.ProgramId == programId);

        Assert.Equal(PurchaseStatus.Refunded, purchase.Status);
        Assert.False(entitlement.IsActive);
        Assert.False(await fx.AccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));

        // History survives — no rows are deleted.
        Assert.True(fx.DbContext.Purchases.Any(p => p.Id == purchase.Id));
        Assert.True(fx.DbContext.Payments.Any(p => p.PurchaseId == purchase.Id));

        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "purchase.refunded");
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "program_access.revoked");
    }

    [Fact]
    public async Task Chargeback_flips_status_and_revokes_access()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);
        await fx.DemoEventHandler.HandleAsync(new TriggerDemoEventCommand(userId, programId, DemoAction.ChargeBack), CancellationToken.None);

        var purchase = fx.DbContext.Purchases.Single(p => p.Id == result.PurchaseId);
        Assert.Equal(PurchaseStatus.Chargeback, purchase.Status);
        Assert.False(await fx.AccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));
    }

    [Fact]
    public async Task Repurchasing_after_refund_reactivates_the_same_entitlement_row()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);
        await fx.DemoEventHandler.HandleAsync(new TriggerDemoEventCommand(userId, programId, DemoAction.Refund), CancellationToken.None);
        Assert.False(await fx.AccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));

        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        Assert.True(await fx.AccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));
        Assert.Single(fx.DbContext.ProgramEntitlements.Where(e => e.UserId == userId && e.ProgramId == programId));
    }

    [Fact]
    public async Task Price_change_after_purchase_leaves_historical_purchase_amount_unchanged()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, offerId, _) = SeedActiveOffer(fx.DbContext, amount: 99.00m);
        var userId = Guid.NewGuid();

        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        // A new price is added for the same offer (P3.35/36: never mutate an existing price row).
        fx.DbContext.ProgramPrices.Add(ProgramPrice.Create(offerId, 149.00m, "RON", DateTime.UtcNow.AddMinutes(1)));
        await fx.DbContext.SaveChangesAsync();

        var purchase = fx.DbContext.Purchases.Single(p => p.Id == result.PurchaseId);
        Assert.Equal(99.00m, purchase.Amount);
    }

    [Fact]
    public async Task Access_is_permanent_across_a_fresh_database_scope()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var userId = Guid.NewGuid();

        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        // A brand-new access-context instance over the same connection — no expiration timer,
        // no background sweep needed; the entitlement row alone determines access.
        var freshAccessContext = new BillingProgramAccessContext(fx.DbContext);
        Assert.True(await freshAccessContext.HasProgramAccessAsync(userId, programId, CancellationToken.None));
    }

    [Fact]
    public async Task Checkout_ignores_any_client_supplied_amount_and_always_uses_the_server_side_offer_price()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext, amount: 250.00m, currency: "EUR");
        var userId = Guid.NewGuid();

        // CreateProgramPurchaseCommand has no amount/currency field at all — the client can only
        // ever send ProgramId + Outcome, so a tampered amount is structurally impossible, not
        // merely validated away.
        var result = await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programId, CheckoutOutcome.Success), CancellationToken.None);

        var purchase = fx.DbContext.Purchases.Single(p => p.Id == result.PurchaseId);
        Assert.Equal(250.00m, purchase.Amount);
        Assert.Equal("EUR", purchase.Currency);
    }

    [Fact]
    public async Task Cross_user_access_check_is_denied()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (programId, _, _) = SeedActiveOffer(fx.DbContext);
        var owner = Guid.NewGuid();
        var otherUser = Guid.NewGuid();

        await fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(owner, programId, CheckoutOutcome.Success), CancellationToken.None);

        Assert.True(await fx.AccessContext.HasProgramAccessAsync(owner, programId, CancellationToken.None));
        Assert.False(await fx.AccessContext.HasProgramAccessAsync(otherUser, programId, CancellationToken.None));
    }

    [Fact]
    public async Task Checkout_with_no_active_offer_is_not_found()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var programWithNoOffer = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            fx.PurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(userId, programWithNoOffer, CheckoutOutcome.Success), CancellationToken.None));
    }
}
