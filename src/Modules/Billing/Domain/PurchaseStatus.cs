namespace BUnited.Modules.Billing.Domain;

/// <summary>docs/ARCHITECTURE.md commerce model: a one-time per-program purchase, not a
/// recurring subscription. Transitions live on <see cref="Entities.Purchase"/> itself.</summary>
public enum PurchaseStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded,
    Chargeback,
}
