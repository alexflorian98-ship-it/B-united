using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>Links a platform <c>UserId</c> to the (fake, for V1 — see ADR-010) payment
/// provider's customer identifier.</summary>
public sealed class PaymentCustomer : IAuditableEntity
{
    private PaymentCustomer()
    {
    }

    public static PaymentCustomer Create(Guid userId, string providerCustomerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProviderCustomerId = providerCustomerId,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string ProviderCustomerId { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
