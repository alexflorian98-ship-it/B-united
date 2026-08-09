using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;

namespace BUnited.Modules.Billing.Tests.Domain;

public sealed class PurchaseTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Purchase CreatePurchase() =>
        Purchase.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 99.00m, "RON");

    [Fact]
    public void New_purchase_starts_as_pending()
    {
        var purchase = CreatePurchase();
        Assert.Equal(PurchaseStatus.Pending, purchase.Status);
        Assert.Null(purchase.CompletedAtUtc);
    }

    [Fact]
    public void MarkSucceeded_from_pending_succeeds_and_stamps_completed_at()
    {
        var purchase = CreatePurchase();
        purchase.MarkSucceeded(UtcNow);

        Assert.Equal(PurchaseStatus.Succeeded, purchase.Status);
        Assert.Equal(UtcNow, purchase.CompletedAtUtc);
    }

    [Fact]
    public void MarkSucceeded_while_already_succeeded_is_a_no_op_transition()
    {
        // A racing duplicate delivery reaching the transition after the idempotency guard must
        // not throw.
        var purchase = CreatePurchase();
        purchase.MarkSucceeded(UtcNow);
        purchase.MarkSucceeded(UtcNow.AddMinutes(5));

        Assert.Equal(PurchaseStatus.Succeeded, purchase.Status);
        Assert.Equal(UtcNow, purchase.CompletedAtUtc); // first completion time is preserved
    }

    [Fact]
    public void MarkFailed_from_pending_succeeds()
    {
        var purchase = CreatePurchase();
        purchase.MarkFailed();
        Assert.Equal(PurchaseStatus.Failed, purchase.Status);
    }

    [Fact]
    public void MarkFailed_after_succeeded_is_rejected()
    {
        var purchase = CreatePurchase();
        purchase.MarkSucceeded(UtcNow);
        Assert.Throws<InvalidOperationException>(purchase.MarkFailed);
    }

    [Fact]
    public void MarkRefunded_requires_succeeded_status()
    {
        var purchase = CreatePurchase();
        Assert.Throws<InvalidOperationException>(purchase.MarkRefunded);
    }

    [Fact]
    public void MarkRefunded_after_succeeded_transitions_to_refunded()
    {
        var purchase = CreatePurchase();
        purchase.MarkSucceeded(UtcNow);
        purchase.MarkRefunded();
        Assert.Equal(PurchaseStatus.Refunded, purchase.Status);
    }

    [Fact]
    public void MarkChargedBack_requires_succeeded_status()
    {
        var purchase = CreatePurchase();
        Assert.Throws<InvalidOperationException>(purchase.MarkChargedBack);
    }

    [Fact]
    public void MarkChargedBack_after_succeeded_transitions_to_chargeback()
    {
        var purchase = CreatePurchase();
        purchase.MarkSucceeded(UtcNow);
        purchase.MarkChargedBack();
        Assert.Equal(PurchaseStatus.Chargeback, purchase.Status);
    }

    [Fact]
    public void Refunding_twice_is_rejected()
    {
        var purchase = CreatePurchase();
        purchase.MarkSucceeded(UtcNow);
        purchase.MarkRefunded();
        Assert.Throws<InvalidOperationException>(purchase.MarkRefunded);
    }
}
