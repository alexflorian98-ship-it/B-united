using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>docs/ARCHITECTURE.md §8 state machine:
/// <code>
/// [*] -> Trialing
/// Trialing -> Active: trial converts (payment succeeds)
/// Trialing -> Expired: trial ends unpaid
/// Active -> PastDue: payment fails
/// Active -> Canceled: user cancels
/// PastDue -> Active: payment recovered
/// PastDue -> Expired: grace period exceeded
/// Canceled -> Expired: paid period ends
/// Expired -> Active: re-subscribe
/// </code>
/// Access-allowed/denied per state is NOT decided here — that is
/// <see cref="Entitlement"/>'s job, computed from a stored cutoff date so access correctly lapses
/// over time without needing a background sweep job to eagerly flip this entity's
/// <see cref="Status"/> (no job-scheduling infrastructure exists in this codebase yet).</summary>
public sealed class Subscription : IAuditableEntity
{
    private Subscription()
    {
    }

    public static Subscription StartTrial(Guid userId, Guid planId, Guid planPriceId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            PlanPriceId = planPriceId,
            Status = SubscriptionStatus.Trialing,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid PlanId { get; private set; }

    public Guid PlanPriceId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateTime? CanceledAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Activate()
    {
        // Active -> Active is a legitimate no-op transition: a recurring payment succeeding for
        // an already-Active subscription (the common case — most "activated" events are just
        // the next period's charge going through) must not be treated as an invalid transition.
        if (Status is not (SubscriptionStatus.Trialing or SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Expired))
        {
            throw new InvalidOperationException($"Cannot activate a subscription from status {Status}.");
        }

        Status = SubscriptionStatus.Active;
        CanceledAt = null;
    }

    public void MarkPastDue()
    {
        if (Status != SubscriptionStatus.Active)
        {
            throw new InvalidOperationException($"Cannot mark past-due a subscription from status {Status}.");
        }

        Status = SubscriptionStatus.PastDue;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status is not (SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Trialing))
        {
            throw new InvalidOperationException($"Cannot cancel a subscription from status {Status}.");
        }

        Status = SubscriptionStatus.Canceled;
        CanceledAt = utcNow;
    }

    public void Expire()
    {
        if (Status == SubscriptionStatus.Expired)
        {
            throw new InvalidOperationException("The subscription is already expired.");
        }

        Status = SubscriptionStatus.Expired;
    }
}
