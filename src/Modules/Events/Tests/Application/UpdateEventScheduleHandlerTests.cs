using BUnited.Modules.Events.Application.UseCases.Admin;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Tests.TestSupport;

namespace BUnited.Modules.Events.Tests.Application;

public sealed class UpdateEventScheduleHandlerTests
{
    [Fact]
    public async Task Moving_the_start_time_reschedules_pending_reminders_but_leaves_sent_ones_alone()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        using var _ = connection;

        var originalStart = DateTime.UtcNow.AddDays(5);
        var @event = Event.Create("ro", originalStart, originalStart.AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example.com/x", null, null);
        dbContext.Events.Add(@event);

        var registration = EventRegistration.Create(@event.Id, Guid.NewGuid(), EventRegistrationStatus.Registered);
        dbContext.EventRegistrations.Add(registration);

        var pendingReminder = EventReminder.Create(registration.Id, @event.Id, EventReminderType.TwentyFourHour, originalStart.AddHours(-24));
        var sentReminder = EventReminder.Create(registration.Id, @event.Id, EventReminderType.OneHour, originalStart.AddHours(-1));
        sentReminder.MarkSent(DateTime.UtcNow);
        dbContext.EventReminders.AddRange(pendingReminder, sentReminder);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateEventScheduleHandler(dbContext);
        var newStart = originalStart.AddDays(3);
        await handler.HandleAsync(new UpdateEventScheduleCommand(
            @event.Id, newStart, newStart.AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example.com/x", null, Guid.NewGuid()),
            CancellationToken.None);

        var reloadedPending = dbContext.EventReminders.Single(r => r.Id == pendingReminder.Id);
        var reloadedSent = dbContext.EventReminders.Single(r => r.Id == sentReminder.Id);

        Assert.Equal(newStart.AddHours(-24), reloadedPending.ScheduledForUtc);
        // The sent reminder's fire time is left as originally scheduled — it's history now, not a live slot.
        Assert.Equal(originalStart.AddHours(-1), reloadedSent.ScheduledForUtc);
    }
}
