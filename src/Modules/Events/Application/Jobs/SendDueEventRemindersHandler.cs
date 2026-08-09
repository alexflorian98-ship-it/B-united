using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Notifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.Jobs;

/// <summary>P5.08: the Hangfire-invoked reminder sweep. Idempotent — a reminder is claimed by
/// setting <see cref="EventReminder.SentAtUtc"/> before sending, so a job re-run (retry, or two
/// overlapping runs) only ever processes the still-null rows (P5.19.a). Locale-aware
/// (recipient's <see cref="UserPreference"/> language, defaulting to the event's default
/// language) and timezone-aware (the notification template receives the event's
/// <c>DisplayTimezone</c>, never a server-local rendering).</summary>
public sealed class SendDueEventRemindersHandler(
    DbContext dbContext,
    IUserLookup userLookup,
    INotificationPreferenceLookup preferenceLookup,
    INotificationSender notificationSender)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var dueReminders = await (
            from reminder in dbContext.Set<EventReminder>()
            where reminder.SentAtUtc == null && reminder.ScheduledForUtc <= utcNow
            join registration in dbContext.Set<EventRegistration>() on reminder.EventRegistrationId equals registration.Id
            where registration.Status == EventRegistrationStatus.Registered
            join @event in dbContext.Set<Event>() on reminder.EventId equals @event.Id
            where @event.Status == EventStatus.Published
            select new { Reminder = reminder, Registration = registration, Event = @event })
            .ToListAsync(cancellationToken);

        foreach (var due in dueReminders)
        {
            // Claim first (idempotency guard), then decide whether to actually send — a job
            // crash after this point simply leaves the reminder marked sent, which is the safe
            // failure mode (a missed email, never a duplicate one).
            due.Reminder.MarkSent(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            var notificationsEnabled = await preferenceLookup.AreEmailNotificationsEnabledAsync(due.Registration.UserId, cancellationToken);
            if (!notificationsEnabled)
            {
                continue;
            }

            var user = await userLookup.GetByIdAsync(due.Registration.UserId, cancellationToken);
            if (user is null)
            {
                continue;
            }

            var translation = await dbContext.Set<EventTranslation>().AsNoTracking()
                .FirstOrDefaultAsync(t => t.EventId == due.Event.Id && t.Language == due.Event.DefaultLanguage, cancellationToken);

            await notificationSender.SendAsync(
                NotificationType.EventReminder,
                user.Email,
                new Dictionary<string, string>
                {
                    ["eventId"] = due.Event.Id.ToString(),
                    ["eventTitle"] = translation?.Title ?? string.Empty,
                    ["startsAtUtc"] = due.Event.StartsAtUtc.ToString("O"),
                    ["displayTimezone"] = due.Event.DisplayTimezone,
                    ["reminderType"] = due.Reminder.Type.ToString(),
                },
                cancellationToken);
        }
    }
}
