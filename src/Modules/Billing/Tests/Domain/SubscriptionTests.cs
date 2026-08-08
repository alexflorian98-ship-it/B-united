using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;

namespace BUnited.Modules.Billing.Tests.Domain;

public sealed class SubscriptionTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_subscription_starts_as_trialing()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
    }

    [Fact]
    public void Activate_from_trialing_succeeds()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void MarkPastDue_requires_active_status()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(subscription.MarkPastDue);
    }

    [Fact]
    public void Activate_while_already_active_is_a_no_op_transition()
    {
        // Regression test: a recurring payment succeeding for an already-Active subscription
        // used to throw InvalidOperationException — found live via curl, not by this suite.
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        subscription.Activate();
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void Activate_recovers_from_past_due()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        subscription.MarkPastDue();
        subscription.Activate();
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void Cancel_sets_canceled_at()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        subscription.Cancel(UtcNow);

        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
        Assert.Equal(UtcNow, subscription.CanceledAt);
    }

    [Fact]
    public void Expire_from_canceled_succeeds()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        subscription.Cancel(UtcNow);
        subscription.Expire();

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Fact]
    public void Expiring_twice_is_rejected()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        subscription.Cancel(UtcNow);
        subscription.Expire();

        Assert.Throws<InvalidOperationException>(subscription.Expire);
    }

    [Fact]
    public void Re_subscribe_activates_from_expired()
    {
        var subscription = Subscription.StartTrial(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscription.Activate();
        subscription.Cancel(UtcNow);
        subscription.Expire();
        subscription.Activate();

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }
}
