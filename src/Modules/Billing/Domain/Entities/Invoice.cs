using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>Simulated invoice metadata for V1 (P3.19) — no provider-hosted PDF; a local
/// invoice/receipt view is generated from these fields.</summary>
public sealed class Invoice : IAuditableEntity
{
    private Invoice()
    {
    }

    public static Invoice Create(Guid subscriptionId, Guid? paymentId, decimal amount, string currency, InvoiceStatus status, DateTime issuedAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            PaymentId = paymentId,
            Amount = amount,
            Currency = currency,
            Status = status,
            IssuedAtUtc = issuedAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public Guid? PaymentId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public InvoiceStatus Status { get; private set; }

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
