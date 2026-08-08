namespace BUnited.Modules.Billing.Application.Configuration;

/// <summary>docs/PROMPT.md §16: "PastDue — access remains available until PaidPeriodEnd +
/// configured grace period (default 3 days)."</summary>
public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    public int GracePeriodDays { get; set; } = 3;

    /// <summary>Shared secret for demo fake-webhook signature validation (P3.07.a). Must never
    /// be a real provider's signing secret — this only ever authenticates our own
    /// server-generated fake events.</summary>
    public string DemoWebhookSecret { get; set; } = string.Empty;
}
