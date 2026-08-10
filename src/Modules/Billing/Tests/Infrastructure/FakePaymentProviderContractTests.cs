using System.Text.Json;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Infrastructure.Payments;

namespace BUnited.Modules.Billing.Tests.Infrastructure;

/// <summary>P7.22.e. No test previously exercised <see cref="FakePaymentProvider"/> through the
/// <see cref="IPaymentProvider"/> interface directly — every other Billing test drives it only
/// indirectly, through a handler. These tests are a regression guard for the implicit contract a
/// future real provider would have to honor: non-empty provider-issued identifiers, decimal
/// amount + explicit currency code (never a client-suppliable float), a real
/// <see cref="ProviderEventType"/> enum value on every produced event, and the documented
/// "no event for a transient failure" contract for ProviderError/Timeout. If
/// <see cref="FakePaymentProvider"/> is ever changed in a way that breaks this shape, this file
/// should fail before a handler-level test does.</summary>
public sealed class FakePaymentProviderContractTests
{
    private static readonly IPaymentProvider Provider = new FakePaymentProvider();

    [Fact]
    public void ProviderName_is_a_stable_non_empty_identifier()
    {
        Assert.False(string.IsNullOrWhiteSpace(Provider.ProviderName));
    }

    [Fact]
    public async Task EnsureCustomerAsync_returns_a_non_empty_provider_customer_reference()
    {
        var userId = Guid.NewGuid();

        var customerReference = await Provider.EnsureCustomerAsync(userId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(customerReference));
    }

    [Fact]
    public async Task EnsureCustomerAsync_is_deterministic_per_user_and_distinct_across_users()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var referenceA1 = await Provider.EnsureCustomerAsync(userA, CancellationToken.None);
        var referenceA2 = await Provider.EnsureCustomerAsync(userA, CancellationToken.None);
        var referenceB = await Provider.EnsureCustomerAsync(userB, CancellationToken.None);

        Assert.Equal(referenceA1, referenceA2);
        Assert.NotEqual(referenceA1, referenceB);
    }

    [Theory]
    [InlineData(CheckoutOutcome.Success, ProviderEventType.PaymentSucceeded)]
    [InlineData(CheckoutOutcome.Decline, ProviderEventType.PaymentFailed)]
    public void SimulateCheckout_on_a_resolving_outcome_returns_a_well_shaped_event(CheckoutOutcome outcome, ProviderEventType expectedType)
    {
        var purchaseId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var providerEvent = Provider.SimulateCheckout(purchaseId, 149.00m, "RON", outcome, utcNow);

        Assert.NotNull(providerEvent);
        AssertWellShapedEvent(providerEvent!, purchaseId, expectedType, 149.00m, "RON", utcNow);
    }

    [Theory]
    [InlineData(CheckoutOutcome.ProviderError)]
    [InlineData(CheckoutOutcome.Timeout)]
    public void SimulateCheckout_on_a_transient_outcome_produces_no_event(CheckoutOutcome outcome)
    {
        var providerEvent = Provider.SimulateCheckout(Guid.NewGuid(), 149.00m, "RON", outcome, DateTime.UtcNow);

        Assert.Null(providerEvent);
    }

    [Theory]
    [InlineData(ProviderEventType.PaymentSucceeded)]
    [InlineData(ProviderEventType.PaymentFailed)]
    [InlineData(ProviderEventType.PaymentRefunded)]
    [InlineData(ProviderEventType.PaymentChargedBack)]
    public void CreateDemoEvent_returns_a_well_shaped_event_for_every_event_type(ProviderEventType type)
    {
        var purchaseId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var providerEvent = Provider.CreateDemoEvent(purchaseId, type, 250.00m, "EUR", utcNow);

        AssertWellShapedEvent(providerEvent, purchaseId, type, 250.00m, "EUR", utcNow);
    }

    [Fact]
    public void CreateDemoEvent_issues_a_distinct_provider_event_id_on_every_call()
    {
        var purchaseId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var first = Provider.CreateDemoEvent(purchaseId, ProviderEventType.PaymentSucceeded, 99.00m, "RON", utcNow);
        var second = Provider.CreateDemoEvent(purchaseId, ProviderEventType.PaymentSucceeded, 99.00m, "RON", utcNow);

        // Idempotency (P3.08) hinges on the provider always minting a unique event id per
        // delivery attempt — a provider that reused ids for genuinely distinct events would make
        // every subsequent event silently drop as a "duplicate".
        Assert.NotEqual(first.ProviderEventId, second.ProviderEventId);
    }

    [Fact]
    public void CreateDemoEvent_payload_is_valid_json_and_never_carries_card_data()
    {
        var providerEvent = Provider.CreateDemoEvent(Guid.NewGuid(), ProviderEventType.PaymentSucceeded, 99.00m, "RON", DateTime.UtcNow);

        using var document = JsonDocument.Parse(providerEvent.PayloadJson);
        var text = providerEvent.PayloadJson.ToLowerInvariant();

        Assert.DoesNotContain("card", text);
        Assert.DoesNotContain("pan", text);
        Assert.DoesNotContain("cvv", text);
        Assert.True(document.RootElement.TryGetProperty("id", out _));
    }

    private static void AssertWellShapedEvent(
        ProviderEvent providerEvent, Guid expectedPurchaseId, ProviderEventType expectedType, decimal expectedAmount, string expectedCurrency, DateTime expectedUtc)
    {
        Assert.False(string.IsNullOrWhiteSpace(providerEvent.ProviderEventId));
        Assert.Equal(expectedType, providerEvent.Type);
        Assert.True(Enum.IsDefined(providerEvent.Type));
        Assert.Equal(expectedPurchaseId, providerEvent.PurchaseId);
        Assert.Equal(expectedAmount, providerEvent.Amount);
        Assert.Equal(expectedCurrency, providerEvent.Currency);
        Assert.Equal(3, providerEvent.Currency!.Length); // ISO 4217 alpha-3, matching the money rule (decimal + explicit currency).
        Assert.Equal(expectedUtc, providerEvent.ProviderTimestampUtc);
        Assert.False(string.IsNullOrWhiteSpace(providerEvent.PayloadJson));
        using var _ = JsonDocument.Parse(providerEvent.PayloadJson);
    }
}
