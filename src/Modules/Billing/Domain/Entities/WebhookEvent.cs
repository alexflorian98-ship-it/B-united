using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>Raw provider-event storage (§17: "raw event storage where appropriate"). Idempotent
/// processing (P3.08) is enforced by a unique index on <see cref="ProviderEventId"/> — the
/// handler checks for an existing row with that ID before doing any state-changing work, so a
/// duplicate delivery is a no-op, not a duplicate side effect.</summary>
public sealed class WebhookEvent : IAuditableEntity
{
    private WebhookEvent()
    {
    }

    public static WebhookEvent Create(string providerEventId, string eventType, Guid? subscriptionId, string payloadJson, DateTime providerTimestampUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProviderEventId = providerEventId,
            EventType = eventType,
            SubscriptionId = subscriptionId,
            PayloadJson = payloadJson,
            ProviderTimestampUtc = providerTimestampUtc,
        };

    public Guid Id { get; private set; }

    public string ProviderEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    /// <summary>Correlates events to a subscription for the out-of-order guard (P3.09) — which
    /// compares a new event's <see cref="ProviderTimestampUtc"/> against the latest processed
    /// event for the *same* subscription, not globally.</summary>
    public Guid? SubscriptionId { get; private set; }

    /// <summary>Raw provider payload. Never card data (§65) — the fake provider never generates
    /// any. Visibility is restricted to technical administrators (P3.22), not the ordinary
    /// support/admin billing view.</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>The event's own timestamp as reported by the provider — used for out-of-order
    /// safety (P3.09), never the arrival/processing time.</summary>
    public DateTime ProviderTimestampUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void MarkProcessed(DateTime utcNow) => ProcessedAtUtc ??= utcNow;
}
