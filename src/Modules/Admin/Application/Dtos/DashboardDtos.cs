namespace BUnited.Modules.Admin.Application.Dtos;

/// <summary>docs/PROMPT.md §38/§442 — the expert dashboard's KPI row and "requires attention"
/// widgets. Deliberately avoids vanity metrics: only the six values named in §442.</summary>
public sealed record AdminDashboardDto(
    DashboardKpiDto Kpis,
    QuestionnaireQueueSummaryDto Questionnaires,
    IReadOnlyList<UpcomingEventDto> UpcomingEvents,
    IReadOnlyList<RecentPurchaseDto> RecentPurchases,
    IReadOnlyList<OpenChatReportDto> OpenChatReports,
    IReadOnlyList<RecentlyPublishedProgramDto> RecentlyPublishedPrograms);

/// <summary><see cref="RevenueByCurrency"/> is a list rather than a single number: purchase
/// amounts are genuinely multi-currency in this codebase (ProgramPrice.Currency is a free-text
/// ISO code chosen per offer, not pinned to one currency platform-wide), so summing across
/// currencies as one number would silently misreport revenue. Grouped by currency instead —
/// the smallest correct fix, per docs/DEVELOPMENT_INSTRUCTIONS.md §5 (money/currency
/// discipline). Sums only purchases whose CURRENT status is <c>Succeeded</c> — a purchase that
/// was later refunded/charged back nets out of revenue automatically (its status has moved on),
/// consistent with <see cref="CompletedPurchases"/> counting the same current-status set.</summary>
public sealed record DashboardKpiDto(
    int CustomersWithPurchases,
    int CompletedPurchases,
    int PendingQuestionnaires,
    int UpcomingEventsCount,
    IReadOnlyList<RevenueByCurrencyDto> RevenueByCurrency);

public sealed record RevenueByCurrencyDto(string Currency, decimal Amount);

public sealed record QuestionnaireQueueSummaryDto(int PendingCount, OldestPendingSubmissionDto? Oldest);

public sealed record OldestPendingSubmissionDto(Guid SubmissionId, Guid UserId, string? UserEmail, DateTime SubmittedAtUtc);

public sealed record UpcomingEventDto(Guid EventId, string? Title, DateTime StartsAtUtc, string DisplayTimezone, int? Capacity);

public sealed record RecentPurchaseDto(
    Guid PurchaseId,
    Guid UserId,
    string? UserEmail,
    Guid ProgramId,
    string? ProgramSlug,
    string? ProgramTitleSnapshot,
    decimal Amount,
    string Currency,
    string Status,
    DateTime? CompletedAtUtc);

public sealed record OpenChatReportDto(Guid ReportId, Guid MessageId, Guid ReporterUserId, string? ReporterEmail, string Reason, DateTime CreatedAtUtc);

public sealed record RecentlyPublishedProgramDto(Guid ProgramId, string Slug, DateTime UpdatedAtUtc);
