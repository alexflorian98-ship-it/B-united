using System.Text.Json;
using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Billing.Application.Abstractions;

namespace BUnited.Modules.Billing.Infrastructure.Payments;

/// <summary>V1's only <see cref="IPaymentProvider"/> implementation (ADR-010) — everything here
/// is a local, deterministic simulation; no network call ever leaves the process. Guarded from
/// ever running in <c>Production</c> by the <see cref="IDemoOnlyAdapter"/> startup check.</summary>
public sealed class FakePaymentProvider : IPaymentProvider, IDemoOnlyAdapter
{
    public string ProviderName => "fake";

    public Task<string> EnsureCustomerAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult($"fake_cus_{userId:N}");

    public ProviderEvent? SimulateCheckout(Guid subscriptionId, decimal amount, string currency, CheckoutOutcome outcome, DateTime utcNow) =>
        outcome switch
        {
            CheckoutOutcome.Success => CreateDemoEvent(subscriptionId, ProviderEventType.SubscriptionActivated, amount, currency, utcNow),
            CheckoutOutcome.Decline => CreateDemoEvent(subscriptionId, ProviderEventType.PaymentFailed, amount, currency, utcNow),
            // A real provider that errors out or times out never successfully delivers a
            // checkout-completed webhook at all — modeled here as "no event produced."
            CheckoutOutcome.ProviderError or CheckoutOutcome.Timeout => null,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

    public ProviderEvent CreateDemoEvent(Guid subscriptionId, ProviderEventType type, decimal amount, string currency, DateTime utcNow)
    {
        var providerEventId = $"fake_evt_{Guid.NewGuid():N}";
        var payload = JsonSerializer.Serialize(new
        {
            id = providerEventId,
            type = type.ToString(),
            subscriptionId,
            amount,
            currency,
            timestamp = utcNow,
        });

        return new ProviderEvent(providerEventId, type, subscriptionId, amount, currency, utcNow, payload);
    }
}
