namespace BUnited.Modules.Billing.Domain;

/// <summary>docs/ARCHITECTURE.md §8 subscription state machine (§16). Transitions live on
/// <see cref="Entities.Subscription"/> itself, not here.</summary>
public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled,
    Expired,
}
