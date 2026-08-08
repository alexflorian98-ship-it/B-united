using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>One billed period of a <see cref="Subscription"/>. <see cref="PaidPeriodEnd"/> is
/// the actual paid-through date used for grace-period math (§16) — normally equal to
/// <see cref="PeriodEnd"/>, but kept as a distinct field since a real provider can report them
/// separately (e.g. a partial/prorated payment).</summary>
public sealed class SubscriptionPeriod : IAuditableEntity
{
    private SubscriptionPeriod()
    {
    }

    public static SubscriptionPeriod Create(Guid subscriptionId, DateTime periodStart, DateTime periodEnd, DateTime paidPeriodEnd) =>
        new()
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PaidPeriodEnd = paidPeriodEnd,
        };

    public Guid Id { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public DateTime PeriodStart { get; private set; }

    public DateTime PeriodEnd { get; private set; }

    public DateTime PaidPeriodEnd { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
