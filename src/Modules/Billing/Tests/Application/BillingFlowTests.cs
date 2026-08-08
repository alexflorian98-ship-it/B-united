using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Configuration;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Infrastructure.Payments;
using BUnited.Modules.Billing.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Billing.Tests.Application;

public sealed class BillingFlowTests
{
    private static readonly IOptions<BillingOptions> DefaultOptions = Options.Create(new BillingOptions { GracePeriodDays = 3 });

    private sealed record Fixture(TestDbContext DbContext, FakeAuditLogger AuditLogger, IPaymentProvider PaymentProvider, ProcessProviderEventHandler EventHandler);

    private static Fixture CreateFixture(out IDisposable connection, BillingOptions? options = null)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var auditLogger = new FakeAuditLogger();
        var paymentProvider = new FakePaymentProvider();
        var eventHandler = new ProcessProviderEventHandler(context, auditLogger, options is null ? DefaultOptions : Options.Create(options));
        return new Fixture(context, auditLogger, paymentProvider, eventHandler);
    }

    private static async Task<(Guid PlanId, Guid PlanPriceId)> SeedPlanAsync(TestDbContext dbContext)
    {
        var plan = Plan.Create("Standard", "Full access.");
        dbContext.Plans.Add(plan);
        var price = PlanPrice.Create(plan.Id, 99.00m, "RON", BillingInterval.Monthly);
        dbContext.PlanPrices.Add(price);
        await dbContext.SaveChangesAsync();
        return (plan.Id, price.Id);
    }

    [Fact]
    public async Task Successful_checkout_activates_subscription_and_grants_access()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();

        var checkoutHandler = new CreateCheckoutHandler(fx.DbContext, fx.PaymentProvider, fx.EventHandler, fx.AuditLogger);
        var result = await checkoutHandler.HandleAsync(new CreateCheckoutCommand(userId, planPriceId, CheckoutOutcome.Success), CancellationToken.None);

        Assert.True(result.Activated);

        var entitlement = fx.DbContext.Entitlements.Single(e => e.UserId == userId);
        Assert.True(entitlement.IsActiveAt(DateTime.UtcNow));
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "payment.webhook_processed");
    }

    [Fact]
    public async Task Declined_checkout_does_not_activate()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();

        var checkoutHandler = new CreateCheckoutHandler(fx.DbContext, fx.PaymentProvider, fx.EventHandler, fx.AuditLogger);
        var result = await checkoutHandler.HandleAsync(new CreateCheckoutCommand(userId, planPriceId, CheckoutOutcome.Decline), CancellationToken.None);

        Assert.False(result.Activated);
        // Trialing + payment fails -> Expired (§8: "trial ends unpaid"), no entitlement granted.
        var subscription = fx.DbContext.Subscriptions.Single(s => s.UserId == userId);
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.False(fx.DbContext.Entitlements.Any(e => e.UserId == userId));
    }

    [Fact]
    public async Task Provider_error_checkout_produces_no_event_and_no_state_change()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();

        var checkoutHandler = new CreateCheckoutHandler(fx.DbContext, fx.PaymentProvider, fx.EventHandler, fx.AuditLogger);
        var result = await checkoutHandler.HandleAsync(new CreateCheckoutCommand(userId, planPriceId, CheckoutOutcome.ProviderError), CancellationToken.None);

        Assert.False(result.Activated);
        var subscription = fx.DbContext.Subscriptions.Single(s => s.UserId == userId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.Empty(fx.DbContext.WebhookEvents);
    }

    [Fact]
    public async Task Duplicate_event_delivery_is_processed_once()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var subscription = Subscription.StartTrial(Guid.NewGuid(), planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        var evt = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionActivated, 99.00m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(evt, CancellationToken.None);
        await fx.EventHandler.HandleAsync(evt, CancellationToken.None);

        Assert.Single(fx.DbContext.WebhookEvents);
        Assert.Single(fx.DbContext.SubscriptionPeriods);
    }

    [Fact]
    public async Task Out_of_order_event_does_not_regress_state()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var subscription = Subscription.StartTrial(Guid.NewGuid(), planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        var utcNow = DateTime.UtcNow;
        var laterActivated = new ProviderEvent("evt-later", ProviderEventType.SubscriptionActivated, subscription.Id, 99m, "RON", utcNow, "{}");
        var earlierFailed = new ProviderEvent("evt-earlier", ProviderEventType.PaymentFailed, subscription.Id, 99m, "RON", utcNow.AddMinutes(-5), "{}");

        await fx.EventHandler.HandleAsync(laterActivated, CancellationToken.None);
        await fx.EventHandler.HandleAsync(earlierFailed, CancellationToken.None);

        var refreshed = fx.DbContext.Subscriptions.Single(s => s.Id == subscription.Id);
        // The earlier (stale) failure must not regress the already-Active subscription.
        Assert.Equal(SubscriptionStatus.Active, refreshed.Status);
        Assert.Equal(2, fx.DbContext.WebhookEvents.Count());
    }

    [Fact]
    public async Task PastDue_access_is_denied_after_grace_period_boundary()
    {
        var fx = CreateFixture(out var connection, new BillingOptions { GracePeriodDays = 3 });
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();
        var subscription = Subscription.StartTrial(userId, planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        var activated = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionActivated, 99m, "RON", DateTime.UtcNow.AddDays(-40));
        await fx.EventHandler.HandleAsync(activated, CancellationToken.None);

        var period = fx.DbContext.SubscriptionPeriods.Single(p => p.SubscriptionId == subscription.Id);

        var failed = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.PaymentFailed, 99m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(failed, CancellationToken.None);

        var entitlement = fx.DbContext.Entitlements.Single(e => e.UserId == userId);
        var graceCutoff = period.PaidPeriodEnd.AddDays(3);

        Assert.True(entitlement.IsActiveAt(graceCutoff.AddDays(-1)), "Access should remain within the grace period.");
        Assert.False(entitlement.IsActiveAt(graceCutoff.AddDays(1)), "Access should be denied once the grace period has passed.");
    }

    [Fact]
    public async Task Canceled_subscription_keeps_access_until_period_end()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();
        var subscription = Subscription.StartTrial(userId, planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        var activated = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionActivated, 99m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(activated, CancellationToken.None);
        var period = fx.DbContext.SubscriptionPeriods.Single(p => p.SubscriptionId == subscription.Id);

        var canceled = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionCanceled, 99m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(canceled, CancellationToken.None);

        var entitlement = fx.DbContext.Entitlements.Single(e => e.UserId == userId);
        Assert.True(entitlement.IsActiveAt(period.PeriodEnd.AddDays(-1)));
        Assert.False(entitlement.IsActiveAt(period.PeriodEnd.AddDays(1)));
    }

    [Fact]
    public async Task Expired_subscription_denies_access_but_preserves_history()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();
        var subscription = Subscription.StartTrial(userId, planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        var activated = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionActivated, 99m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(activated, CancellationToken.None);

        var expired = fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionExpired, 99m, "RON", DateTime.UtcNow);
        await fx.EventHandler.HandleAsync(expired, CancellationToken.None);

        var entitlement = fx.DbContext.Entitlements.Single(e => e.UserId == userId);
        Assert.False(entitlement.IsActiveAt(DateTime.UtcNow));

        // Historical data survives.
        Assert.True(fx.DbContext.Subscriptions.Any(s => s.Id == subscription.Id));
        Assert.True(fx.DbContext.Payments.Any(p => p.SubscriptionId == subscription.Id));
    }

    [Fact]
    public async Task Re_subscribing_after_expiration_restores_access()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();
        var subscription = Subscription.StartTrial(userId, planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        await fx.EventHandler.HandleAsync(fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionActivated, 99m, "RON", DateTime.UtcNow), CancellationToken.None);
        await fx.EventHandler.HandleAsync(fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionExpired, 99m, "RON", DateTime.UtcNow), CancellationToken.None);
        await fx.EventHandler.HandleAsync(fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionRenewed, 99m, "RON", DateTime.UtcNow), CancellationToken.None);

        var refreshed = fx.DbContext.Subscriptions.Single(s => s.Id == subscription.Id);
        var entitlement = fx.DbContext.Entitlements.Single(e => e.UserId == userId);

        Assert.Equal(SubscriptionStatus.Active, refreshed.Status);
        Assert.True(entitlement.IsActiveAt(DateTime.UtcNow));
        Assert.Equal(2, fx.DbContext.SubscriptionPeriods.Count(p => p.SubscriptionId == subscription.Id));
    }

    [Fact]
    public async Task Renewing_a_canceled_but_not_yet_expired_subscription_is_a_clean_business_error()
    {
        // Regression test: this used to throw an uncaught InvalidOperationException (500)
        // instead of a BusinessRuleAppException (400) — found live via curl, not by this suite.
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();
        var subscription = Subscription.StartTrial(userId, planId, planPriceId);
        fx.DbContext.Subscriptions.Add(subscription);
        await fx.DbContext.SaveChangesAsync();

        await fx.EventHandler.HandleAsync(fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionActivated, 99m, "RON", DateTime.UtcNow), CancellationToken.None);
        await fx.EventHandler.HandleAsync(fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionCanceled, 99m, "RON", DateTime.UtcNow), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BUnited.BuildingBlocks.Application.Errors.BusinessRuleAppException>(() =>
            fx.EventHandler.HandleAsync(fx.PaymentProvider.CreateDemoEvent(subscription.Id, ProviderEventType.SubscriptionRenewed, 99m, "RON", DateTime.UtcNow), CancellationToken.None));
        Assert.Equal("SUBSCRIPTION_STATUS_TRANSITION_INVALID", ex.Code);
    }

    [Fact]
    public async Task Checkout_for_a_user_with_an_active_subscription_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var (planId, planPriceId) = await SeedPlanAsync(fx.DbContext);
        var userId = Guid.NewGuid();

        var checkoutHandler = new CreateCheckoutHandler(fx.DbContext, fx.PaymentProvider, fx.EventHandler, fx.AuditLogger);
        await checkoutHandler.HandleAsync(new CreateCheckoutCommand(userId, planPriceId, CheckoutOutcome.Success), CancellationToken.None);

        await Assert.ThrowsAsync<BUnited.BuildingBlocks.Application.Errors.BusinessRuleAppException>(() =>
            checkoutHandler.HandleAsync(new CreateCheckoutCommand(userId, planPriceId, CheckoutOutcome.Success), CancellationToken.None));
    }
}
