namespace BUnited.Modules.Billing.Application.Dtos;

public sealed record PaymentDto(Guid Id, decimal Amount, string Currency, string Status, DateTime OccurredAtUtc);

public sealed record InvoiceDto(Guid Id, decimal Amount, string Currency, string Status, DateTime IssuedAtUtc);
public sealed record MyInvoiceDto(Guid Id, Guid PurchaseId, Guid ProgramId, string? ProgramTitleSnapshot, decimal Amount, string Currency, string Status, DateTime IssuedAtUtc);

public sealed record PurchaseDto(Guid Id, Guid ProgramId, string? ProgramTitleSnapshot, decimal Amount, string Currency, string Status, DateTime CreatedAt, DateTime? CompletedAtUtc);

public sealed record ProgramEntitlementDto(Guid ProgramId, string Status, DateTime GrantedAtUtc, DateTime? RevokedAtUtc);

public sealed record CheckoutResultDto(Guid PurchaseId, Guid ProgramId, string Status);

public sealed record PurchaseSummaryDto(
    Guid PurchaseId,
    Guid UserId,
    string? Email,
    Guid ProgramId,
    string? ProgramTitleSnapshot,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAtUtc);

public sealed record PurchaseListResult(IReadOnlyList<PurchaseSummaryDto> Items, int TotalCount);

public sealed record WebhookEventSummaryDto(Guid Id, string EventType, DateTime ProviderTimestampUtc, DateTime? ProcessedAtUtc, string? PayloadJson);

public sealed record PurchaseDetailDto(
    Guid PurchaseId,
    Guid UserId,
    string? Email,
    Guid ProgramId,
    string? ProgramTitleSnapshot,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAtUtc,
    string? EntitlementStatus,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<InvoiceDto> Invoices,
    IReadOnlyList<WebhookEventSummaryDto> WebhookTimeline);

public sealed record ProgramPriceDto(Guid Id, decimal Amount, string Currency, DateTime CreatedAtUtc);

public sealed record ProgramOfferSummaryDto(
    Guid Id,
    Guid ProgramId,
    string Status,
    decimal? CurrentAmount,
    string? CurrentCurrency,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ProgramOfferListResult(IReadOnlyList<ProgramOfferSummaryDto> Items, int TotalCount);

public sealed record ProgramOfferDetailDto(
    Guid Id,
    Guid ProgramId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ProgramPriceDto> PriceHistory,
    int PurchaseCount,
    int SucceededPurchaseCount);

public sealed record CreateProgramOfferResultDto(Guid ProgramOfferId, Guid ProgramPriceId);
