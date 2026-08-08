namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>Money is always <c>decimal</c> with an explicit currency (docs/PROMPT.md §63) —
/// never <c>float</c>/<c>double</c>.</summary>
public sealed class PlanPrice
{
    private PlanPrice()
    {
    }

    public static PlanPrice Create(Guid planId, decimal amount, string currency, BillingInterval interval) =>
        new()
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Amount = amount,
            Currency = currency,
            Interval = interval,
            IsActive = true,
        };

    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public BillingInterval Interval { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;
}
