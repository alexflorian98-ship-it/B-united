using BUnited.Modules.Events.Application.Jobs;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Tests.TestSupport;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Notifications.Contracts;

namespace BUnited.Modules.Events.Tests.Application;

public sealed class EventReminderJobTests
{
    private sealed record Fixture(
        TestDbContext DbContext,
        FakeUserLookup UserLookup,
        FakeNotificationPreferenceLookup PreferenceLookup,
        FakeNotificationSender NotificationSender,
        SendDueEventRemindersHandler JobHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var userLookup = new FakeUserLookup();
        var preferenceLookup = new FakeNotificationPreferenceLookup();
        var notificationSender = new FakeNotificationSender();
        var jobHandler = new SendDueEventRemindersHandler(context, userLookup, preferenceLookup, notificationSender);
        return new Fixture(context, userLookup, preferenceLookup, notificationSender, jobHandler);
    }

    private static (Event Event, EventRegistration Registration) SeedDueReminder(TestDbContext dbContext, Guid userId, EventReminderType type, bool sent = false)
    {
        var startsAtUtc = DateTime.UtcNow.AddHours(2);
        var @event = Event.Create("ro", startsAtUtc, startsAtUtc.AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example.com/x", null, null);
        @event.Publish(DateTime.UtcNow, Guid.NewGuid());
        dbContext.Events.Add(@event);
        dbContext.EventTranslations.Add(EventTranslation.Create(@event.Id, "ro", "Live Q&A", "Description"));

        var registration = EventRegistration.Create(@event.Id, userId, EventRegistrationStatus.Registered);
        dbContext.EventRegistrations.Add(registration);

        // Scheduled in the past relative to "now" so the job sees it as due.
        var reminder = EventReminder.Create(registration.Id, @event.Id, type, DateTime.UtcNow.AddMinutes(-5));
        if (sent)
        {
            reminder.MarkSent(DateTime.UtcNow.AddMinutes(-4));
        }

        dbContext.EventReminders.Add(reminder);
        dbContext.SaveChanges();

        return (@event, registration);
    }

    [Fact]
    public async Task Due_reminder_is_sent_and_marked_sent()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        fx.UserLookup.Users[userId] = new UserSummary(userId, "client@example.com", null);
        var (@event, _) = SeedDueReminder(fx.DbContext, userId, EventReminderType.OneHour);

        await fx.JobHandler.HandleAsync(CancellationToken.None);

        Assert.Single(fx.NotificationSender.Sent);
        Assert.Equal(NotificationType.EventReminder, fx.NotificationSender.Sent[0].Type);
        Assert.Equal("client@example.com", fx.NotificationSender.Sent[0].RecipientEmail);

        var reminder = fx.DbContext.EventReminders.Single(r => r.EventId == @event.Id);
        Assert.NotNull(reminder.SentAtUtc);
    }

    [Fact]
    public async Task Re_running_the_job_never_sends_a_duplicate_reminder()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        fx.UserLookup.Users[userId] = new UserSummary(userId, "client@example.com", null);
        SeedDueReminder(fx.DbContext, userId, EventReminderType.OneHour);

        await fx.JobHandler.HandleAsync(CancellationToken.None);
        await fx.JobHandler.HandleAsync(CancellationToken.None);
        await fx.JobHandler.HandleAsync(CancellationToken.None);

        Assert.Single(fx.NotificationSender.Sent);
    }

    [Fact]
    public async Task Already_sent_reminder_is_not_reprocessed()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        fx.UserLookup.Users[userId] = new UserSummary(userId, "client@example.com", null);
        SeedDueReminder(fx.DbContext, userId, EventReminderType.OneHour, sent: true);

        await fx.JobHandler.HandleAsync(CancellationToken.None);

        Assert.Empty(fx.NotificationSender.Sent);
    }

    [Fact]
    public async Task Reminder_is_suppressed_but_still_marked_sent_when_user_opted_out()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        fx.UserLookup.Users[userId] = new UserSummary(userId, "client@example.com", null);
        fx.PreferenceLookup.OptedOutUsers.Add(userId);
        var (@event, _) = SeedDueReminder(fx.DbContext, userId, EventReminderType.OneHour);

        await fx.JobHandler.HandleAsync(CancellationToken.None);

        Assert.Empty(fx.NotificationSender.Sent);
        var reminder = fx.DbContext.EventReminders.Single(r => r.EventId == @event.Id);
        Assert.NotNull(reminder.SentAtUtc);
    }

    [Fact]
    public async Task Reminder_for_a_canceled_registration_is_never_sent()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        fx.UserLookup.Users[userId] = new UserSummary(userId, "client@example.com", null);
        var (_, registration) = SeedDueReminder(fx.DbContext, userId, EventReminderType.OneHour);
        registration.Cancel(DateTime.UtcNow);
        fx.DbContext.SaveChanges();

        await fx.JobHandler.HandleAsync(CancellationToken.None);

        Assert.Empty(fx.NotificationSender.Sent);
    }
}
