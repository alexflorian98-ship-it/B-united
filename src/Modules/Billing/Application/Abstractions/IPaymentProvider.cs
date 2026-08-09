namespace BUnited.Modules.Billing.Application.Abstractions;

public enum CheckoutOutcome
{
    Success,
    Decline,
    ProviderError,
    Timeout,
}

/// <summary>The exact provider-neutral event types this application understands, mapped from
/// (fake, for V1 — ADR-010) provider events to <c>Purchase</c> state transitions. A one-time
/// purchase has no recurring lifecycle, so this vocabulary is deliberately smaller than the old
/// subscription model's.</summary>
public enum ProviderEventType
{
    PaymentSucceeded,
    PaymentFailed,
    PaymentRefunded,
    PaymentChargedBack,
}

public sealed record ProviderEvent(string ProviderEventId, ProviderEventType Type, Guid PurchaseId, decimal? Amount, string? Currency, DateTime ProviderTimestampUtc, string PayloadJson);

/// <summary>docs/PROMPT.md §17. V1's only implementation is <c>FakePaymentProvider</c>
/// (ADR-010) — nothing outside this interface's callers knows that.</summary>
public interface IPaymentProvider
{
    string ProviderName { get; }

    Task<string> EnsureCustomerAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Simulates a checkout attempt against the selected <paramref name="outcome"/> and
    /// returns the resulting provider event, or <see langword="null"/> for outcomes that never
    /// produce a webhook (ProviderError/Timeout — the real-world equivalent of the provider
    /// never getting back to us at all).</summary>
    ProviderEvent? SimulateCheckout(Guid purchaseId, decimal amount, string currency, CheckoutOutcome outcome, DateTime utcNow);

    ProviderEvent CreateDemoEvent(Guid purchaseId, ProviderEventType type, decimal amount, string currency, DateTime utcNow);
}
