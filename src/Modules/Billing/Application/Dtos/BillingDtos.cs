namespace BUnited.Modules.Billing.Application.Dtos;

public sealed record PlanDto(Guid Id, string Name, string Description, IReadOnlyList<PlanPriceDto> Prices);

public sealed record PlanPriceDto(Guid Id, decimal Amount, string Currency, string Interval);

public sealed record SubscriptionPeriodDto(DateTime PeriodStart, DateTime PeriodEnd, DateTime PaidPeriodEnd);

public sealed record PaymentDto(Guid Id, decimal Amount, string Currency, string Status, DateTime OccurredAtUtc);

public sealed record InvoiceDto(Guid Id, decimal Amount, string Currency, string Status, DateTime IssuedAtUtc);

public sealed record MyBillingStatusDto(
    Guid? SubscriptionId,
    string? PlanName,
    string? Status,
    SubscriptionPeriodDto? CurrentPeriod,
    bool HasActiveAccess,
    DateTime? AccessUntilUtc,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<InvoiceDto> Invoices);

public sealed record CheckoutResultDto(Guid SubscriptionId, bool Activated, string? DeclineReason);

public sealed record SubscriberSummaryDto(
    Guid UserId,
    string? Email,
    Guid SubscriptionId,
    string Status,
    DateTime? CurrentPeriodEnd,
    DateTime? AccessUntilUtc,
    string PaymentState,
    DateTime CreatedAt);

public sealed record SubscriberListResult(IReadOnlyList<SubscriberSummaryDto> Items, int TotalCount);

public sealed record WebhookEventSummaryDto(Guid Id, string EventType, DateTime ProviderTimestampUtc, DateTime? ProcessedAtUtc, string? PayloadJson);

public sealed record SubscriptionDetailDto(
    Guid SubscriptionId,
    Guid UserId,
    string? Email,
    string PlanName,
    string Status,
    SubscriptionPeriodDto? CurrentPeriod,
    DateTime? AccessUntilUtc,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<InvoiceDto> Invoices,
    IReadOnlyList<WebhookEventSummaryDto> WebhookTimeline);
