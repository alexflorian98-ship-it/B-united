using BUnited.Modules.Admin.Application.Dtos;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Admin.Application.UseCases;

/// <summary>docs/PROMPT.md §442 ("Expert dashboard emphasizes actions requiring attention...") —
/// this is the ADR-007 read-only cross-module projection: it queries entities owned by
/// Billing, Questionnaires, Events, Chat and Content directly through the shared
/// <see cref="DbContext"/> (never their Domain behavior, never a write), resolving client
/// identities only through <see cref="IUserLookup"/> so it does not need Identity's Domain
/// layer either. Every query here is <c>AsNoTracking</c> and read-only end to end; see
/// <c>GetDashboardHandlerTests.Does_not_change_any_row_in_any_module</c> for the regression
/// test proving that.</summary>
public sealed class GetDashboardHandler(DbContext dbContext, IUserLookup userLookup, TimeProvider timeProvider)
{
    private const int UpcomingEventsLimit = 5;
    private const int RecentPurchasesLimit = 10;
    private const int OpenReportsLimit = 10;
    private const int RecentlyPublishedProgramsLimit = 5;

    public async Task<AdminDashboardDto> HandleAsync(CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var pendingSubmissions = await dbContext.Set<QuestionnaireSubmission>().AsNoTracking()
            .Where(s => s.Status == QuestionnaireSubmissionStatus.Submitted)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        var upcomingEvents = await dbContext.Set<Event>().AsNoTracking()
            .Where(e => e.Status == EventStatus.Published && e.StartsAtUtc > utcNow)
            .OrderBy(e => e.StartsAtUtc)
            .Take(UpcomingEventsLimit)
            .ToListAsync(cancellationToken);
        var upcomingEventsCount = await dbContext.Set<Event>().AsNoTracking()
            .CountAsync(e => e.Status == EventStatus.Published && e.StartsAtUtc > utcNow, cancellationToken);

        var recentPurchases = await dbContext.Set<Purchase>().AsNoTracking()
            .Where(p => p.Status == PurchaseStatus.Succeeded || p.Status == PurchaseStatus.Refunded || p.Status == PurchaseStatus.Chargeback)
            .OrderByDescending(p => p.CreatedAt)
            .Take(RecentPurchasesLimit)
            .ToListAsync(cancellationToken);

        var completedPurchasesCount = await dbContext.Set<Purchase>().AsNoTracking()
            .CountAsync(p => p.Status == PurchaseStatus.Succeeded, cancellationToken);
        var customersWithPurchases = await dbContext.Set<Purchase>().AsNoTracking()
            .Where(p => p.Status == PurchaseStatus.Succeeded)
            .Select(p => p.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var revenueByCurrency = await dbContext.Set<Purchase>().AsNoTracking()
            .Where(p => p.Status == PurchaseStatus.Succeeded)
            .GroupBy(p => p.Currency)
            .Select(g => new RevenueByCurrencyDto(g.Key, g.Sum(p => p.Amount)))
            .ToListAsync(cancellationToken);

        var openReports = await dbContext.Set<Report>().AsNoTracking()
            .Where(r => r.Status == ReportStatus.Open)
            .OrderByDescending(r => r.CreatedAt)
            .Take(OpenReportsLimit)
            .ToListAsync(cancellationToken);

        var recentlyPublishedPrograms = await dbContext.Set<Program>().AsNoTracking()
            .Where(p => p.Status == ContentStatus.Published)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(RecentlyPublishedProgramsLimit)
            .Select(p => new RecentlyPublishedProgramDto(p.Id, p.Slug, p.UpdatedAt))
            .ToListAsync(cancellationToken);

        var eventTranslations = await dbContext.Set<EventTranslation>().AsNoTracking()
            .Where(t => upcomingEvents.Select(e => e.Id).Contains(t.EventId))
            .ToListAsync(cancellationToken);

        var userIds = pendingSubmissions.Select(s => s.UserId)
            .Concat(recentPurchases.Select(p => p.UserId))
            .Concat(openReports.Select(r => r.ReporterId))
            .Distinct()
            .ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        var programIds = recentPurchases.Select(p => p.ProgramId).Distinct().ToList();
        var purchaseProgramSlugs = await dbContext.Set<Program>().AsNoTracking()
            .Where(p => programIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Slug })
            .ToDictionaryAsync(p => p.Id, p => p.Slug, cancellationToken);

        var oldest = pendingSubmissions.FirstOrDefault();

        return new AdminDashboardDto(
            Kpis: new DashboardKpiDto(customersWithPurchases, completedPurchasesCount, pendingSubmissions.Count, upcomingEventsCount, revenueByCurrency),
            Questionnaires: new QuestionnaireQueueSummaryDto(
                pendingSubmissions.Count,
                oldest is null ? null : new OldestPendingSubmissionDto(oldest.Id, oldest.UserId, ResolveEmail(users, oldest.UserId), oldest.SubmittedAt!.Value)),
            UpcomingEvents: upcomingEvents.Select(e =>
            {
                var translation = eventTranslations.FirstOrDefault(t => t.EventId == e.Id && t.Language == e.DefaultLanguage)
                    ?? eventTranslations.FirstOrDefault(t => t.EventId == e.Id);
                return new UpcomingEventDto(e.Id, translation?.Title, e.StartsAtUtc, e.DisplayTimezone, e.Capacity);
            }).ToList(),
            RecentPurchases: recentPurchases.Select(p => new RecentPurchaseDto(
                p.Id,
                p.UserId,
                ResolveEmail(users, p.UserId),
                p.ProgramId,
                purchaseProgramSlugs.GetValueOrDefault(p.ProgramId),
                p.ProgramTitleSnapshot,
                p.Amount,
                p.Currency,
                p.Status.ToString(),
                p.CompletedAtUtc)).ToList(),
            OpenChatReports: openReports.Select(r => new OpenChatReportDto(r.Id, r.MessageId, r.ReporterId, ResolveEmail(users, r.ReporterId), r.Reason, r.CreatedAt)).ToList(),
            RecentlyPublishedPrograms: recentlyPublishedPrograms);
    }

    private static string? ResolveEmail(IReadOnlyDictionary<Guid, UserSummary> users, Guid userId) =>
        users.TryGetValue(userId, out var user) ? user.Email : null;
}
