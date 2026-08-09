using BUnited.Modules.Admin.Application.UseCases;
using BUnited.Modules.Admin.Tests.TestSupport;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Admin.Tests.Application;

/// <summary>docs/PROMPT.md §442 — proves the expert dashboard's KPI numbers and "requires
/// attention" widgets are computed correctly from real rows owned by five different modules,
/// and that the read model (ADR-007) never mutates any of them.</summary>
public sealed class GetDashboardHandlerTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider Clock = new FakeTimeProvider(Now);

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private static async Task<(TestDbContext DbContext, IDisposable Connection)> SeedAsync()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();

        // Content: one published program (purchases point at it) + one more-recently-published
        // program that should surface in "recently published content".
        var domain = ContentDomain.Create(Guid.NewGuid(), "wellbeing", 0);
        var purchasedProgram = Program.Create(domain.Id, "resilience-101", "ro", Actor);
        purchasedProgram.Publish(Actor);
        var recentlyPublishedProgram = Program.Create(domain.Id, "mindful-mornings", "ro", Actor);
        recentlyPublishedProgram.Publish(Actor);
        var draftProgram = Program.Create(domain.Id, "still-drafting", "ro", Actor);
        dbContext.AddRange(domain, purchasedProgram, recentlyPublishedProgram, draftProgram);

        // Questionnaires: two Submitted (oldest = earlier SubmittedAt), one Answered (must not count).
        var questionnaire = Questionnaire.Create(purchasedProgram.Id, "ro", Actor);
        dbContext.Add(questionnaire);
        var oldestClient = Guid.NewGuid();
        var newerClient = Guid.NewGuid();
        var answeredClient = Guid.NewGuid();
        var oldestSubmission = QuestionnaireSubmission.Start(oldestClient, questionnaire.Id);
        oldestSubmission.MarkStarted(Now.AddDays(-5));
        oldestSubmission.Submit(Now.AddDays(-5));
        var newerSubmission = QuestionnaireSubmission.Start(newerClient, questionnaire.Id);
        newerSubmission.MarkStarted(Now.AddDays(-1));
        newerSubmission.Submit(Now.AddDays(-1));
        var answeredSubmission = QuestionnaireSubmission.Start(answeredClient, questionnaire.Id);
        answeredSubmission.MarkStarted(Now.AddDays(-10));
        answeredSubmission.Submit(Now.AddDays(-10));
        answeredSubmission.MarkAnswered(Now.AddDays(-9));
        dbContext.AddRange(oldestSubmission, newerSubmission, answeredSubmission);

        // Billing: two Succeeded purchases in different currencies (proves revenue is grouped,
        // never silently summed across currencies), one Refunded (counts as "recent" but not as
        // a completed/KPI purchase), one Pending (must be excluded entirely).
        var offer = ProgramOffer.Create(purchasedProgram.Id);
        var ronPrice = ProgramPrice.Create(offer.Id, 100m, "RON", Now.AddDays(-30));
        var eurPrice = ProgramPrice.Create(offer.Id, 20m, "EUR", Now.AddDays(-30));
        var buyerOne = Guid.NewGuid();
        var buyerTwo = Guid.NewGuid();
        var refundedBuyer = Guid.NewGuid();
        var pendingBuyer = Guid.NewGuid();
        var purchaseOne = Purchase.Create(buyerOne, purchasedProgram.Id, offer.Id, ronPrice.Id, 100m, "RON");
        purchaseOne.MarkSucceeded(Now.AddDays(-2));
        var purchaseTwo = Purchase.Create(buyerTwo, purchasedProgram.Id, offer.Id, eurPrice.Id, 20m, "EUR");
        purchaseTwo.MarkSucceeded(Now.AddDays(-3));
        var refundedPurchase = Purchase.Create(refundedBuyer, purchasedProgram.Id, offer.Id, ronPrice.Id, 100m, "RON");
        refundedPurchase.MarkSucceeded(Now.AddDays(-20));
        refundedPurchase.MarkRefunded();
        var pendingPurchase = Purchase.Create(pendingBuyer, purchasedProgram.Id, offer.Id, ronPrice.Id, 100m, "RON");
        dbContext.AddRange(offer, ronPrice, eurPrice, purchaseOne, purchaseTwo, refundedPurchase, pendingPurchase);

        // Events: one published+upcoming, one published+past (must be excluded), one draft (excluded).
        var upcomingEvent = Event.Create("ro", Now.AddDays(10), Now.AddDays(10).AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example", 20, Actor);
        upcomingEvent.Publish(Now.AddDays(-1), Actor);
        var upcomingTranslation = EventTranslation.Create(upcomingEvent.Id, "ro", "Atelier de reziliență", "Descriere");
        var pastEvent = Event.Create("ro", Now.AddDays(-10), Now.AddDays(-10).AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example", 20, Actor);
        pastEvent.Publish(Now.AddDays(-11), Actor);
        var draftEvent = Event.Create("ro", Now.AddDays(20), Now.AddDays(20).AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example", 20, Actor);
        dbContext.AddRange(upcomingEvent, upcomingTranslation, pastEvent, draftEvent);

        // Chat: one open report (surfaces), one already-resolved report (excluded).
        var room = ChatRoom.Create(purchasedProgram.Id, "general", "General", Actor);
        var reportedMessage = Message.Create(room.Id, buyerOne, "hello");
        var resolvedMessage = Message.Create(room.Id, buyerTwo, "also hello");
        var openReport = Report.Create(reportedMessage.Id, buyerTwo, "spam");
        var resolvedReport = Report.Create(resolvedMessage.Id, buyerOne, "spam");
        resolvedReport.Resolve(ReportStatus.Dismissed, Actor, Now.AddDays(-1));
        dbContext.AddRange(room, reportedMessage, resolvedMessage, openReport, resolvedReport);

        await dbContext.SaveChangesAsync();

        // UpdatedAt is stamped by AuditableEntitySaveChangesInterceptor at SaveChanges time, so
        // publish/save the "more recent" program last to guarantee its UpdatedAt sorts later.
        recentlyPublishedProgram.Reorder(1, Actor);
        await dbContext.SaveChangesAsync();

        return (dbContext, connection);
    }

    [Fact]
    public async Task Computes_kpis_and_widgets_from_five_modules_correctly()
    {
        var (dbContext, connection) = await SeedAsync();
        using var _ = connection;

        var result = await new GetDashboardHandler(dbContext, new FakeUserLookup(), Clock).HandleAsync(CancellationToken.None);

        Assert.Equal(2, result.Kpis.CustomersWithPurchases); // buyerOne, buyerTwo currently Succeeded; refundedBuyer no longer is, pendingBuyer never was
        Assert.Equal(2, result.Kpis.CompletedPurchases); // purchaseOne, purchaseTwo currently Succeeded; refundedPurchase moved on to Refunded
        Assert.Equal(2, result.Kpis.PendingQuestionnaires);
        Assert.Equal(1, result.Kpis.UpcomingEventsCount);

        Assert.Equal(2, result.Kpis.RevenueByCurrency.Count);
        Assert.Equal(100m, result.Kpis.RevenueByCurrency.Single(r => r.Currency == "RON").Amount); // purchaseOne only — refundedPurchase moved off Succeeded
        Assert.Equal(20m, result.Kpis.RevenueByCurrency.Single(r => r.Currency == "EUR").Amount);

        Assert.Equal(2, result.Questionnaires.PendingCount);
        Assert.NotNull(result.Questionnaires.Oldest);
        Assert.Equal(Now.AddDays(-5), result.Questionnaires.Oldest!.SubmittedAtUtc);

        var upcoming = Assert.Single(result.UpcomingEvents);
        Assert.Equal("Atelier de reziliență", upcoming.Title);

        Assert.Equal(3, result.RecentPurchases.Count); // Succeeded + Refunded, Pending excluded
        Assert.DoesNotContain(result.RecentPurchases, p => p.Status == "Pending");
        Assert.Contains(result.RecentPurchases, p => p.Status == "Refunded");
        Assert.All(result.RecentPurchases, p => Assert.Equal("resilience-101", p.ProgramSlug));

        var report = Assert.Single(result.OpenChatReports);
        Assert.Equal("spam", report.Reason);

        Assert.Equal(2, result.RecentlyPublishedPrograms.Count);
        Assert.Equal("mindful-mornings", result.RecentlyPublishedPrograms[0].Slug);
    }

    [Fact]
    public async Task Never_writes_to_any_row_it_reads()
    {
        var (dbContext, connection) = await SeedAsync();
        using var _ = connection;

        var purchaseCountBefore = await dbContext.Purchases.CountAsync();
        var submissionCountBefore = await dbContext.QuestionnaireSubmissions.CountAsync();
        var reportCountBefore = await dbContext.Reports.CountAsync();
        var eventCountBefore = await dbContext.Events.CountAsync();
        var programCountBefore = await dbContext.Programs.CountAsync();

        await new GetDashboardHandler(dbContext, new FakeUserLookup(), Clock).HandleAsync(CancellationToken.None);

        Assert.DoesNotContain(dbContext.ChangeTracker.Entries(), e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged && e.State != Microsoft.EntityFrameworkCore.EntityState.Detached);
        Assert.Equal(purchaseCountBefore, await dbContext.Purchases.CountAsync());
        Assert.Equal(submissionCountBefore, await dbContext.QuestionnaireSubmissions.CountAsync());
        Assert.Equal(reportCountBefore, await dbContext.Reports.CountAsync());
        Assert.Equal(eventCountBefore, await dbContext.Events.CountAsync());
        Assert.Equal(programCountBefore, await dbContext.Programs.CountAsync());
    }
}
