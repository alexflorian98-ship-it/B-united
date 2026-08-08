using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

public sealed class Payment : IAuditableEntity
{
    private Payment()
    {
    }

    public static Payment Create(Guid subscriptionId, decimal amount, string currency, PaymentStatus status, string providerPaymentId, DateTime occurredAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Amount = amount,
            Currency = currency,
            Status = status,
            ProviderPaymentId = providerPaymentId,
            OccurredAtUtc = occurredAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; }

    public string ProviderPaymentId { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
