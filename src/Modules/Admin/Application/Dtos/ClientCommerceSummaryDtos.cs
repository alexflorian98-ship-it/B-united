namespace BUnited.Modules.Admin.Application.Dtos;

public sealed record ClientCommerceSummaryDto(
    IReadOnlyList<ClientPurchaseSummaryDto> Purchases,
    IReadOnlyList<ClientEntitlementSummaryDto> Entitlements);

public sealed record ClientPurchaseSummaryDto(
    Guid PurchaseId,
    Guid ProgramId,
    string? ProgramSlug,
    string? ProgramTitleSnapshot,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAtUtc);

public sealed record ClientEntitlementSummaryDto(
    Guid ProgramId,
    string? ProgramSlug,
    string Status,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc);
