using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Notifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Client;

/// <summary>P5.06.b: canceling a Registered (seat-holding) registration promotes the oldest
/// eligible waitlisted user. Canceling a Waitlisted registration is a plain no-op beyond the
/// cancellation itself — nobody needs to move up because no seat was freed.
///
/// P5.12.b: a promoted user is notified at promotion time, not left to discover the status
/// change on their next page load. Reuses <see cref="NotificationType.EventRegistrationConfirmed"/>
/// rather than adding a new enum member — docs/PROMPT.md §32's notification-type list is
/// spec-fixed, and a promotion is semantically the same event a fresh Registered outcome is
/// (see <see cref="RegisterForEventHandler"/>, which sends the identical type/payload shape).
/// Sent unconditionally, like <see cref="RegisterForEventHandler"/>'s own confirmation send —
/// not gated by <see cref="INotificationPreferenceLookup"/> the way <c>EventReminder</c> is,
/// since a registration-status confirmation is transactional, not a disable-able courtesy
/// notification (docs/PROMPT.md §32: "Security and transactional notifications cannot be
/// disabled").</summary>
public sealed class CancelRegistrationHandler(DbContext dbContext, IUserLookup userLookup, INotificationSender notificationSender)
{
    public async Task HandleAsync(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var registration = await dbContext.Set<EventRegistration>()
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken)
            ?? throw new NotFoundAppException("You are not registered for this event.");

        if (registration.Status == EventRegistrationStatus.Canceled)
        {
            return;
        }

        var wasRegistered = registration.Status == EventRegistrationStatus.Registered;
        var utcNow = DateTime.UtcNow;
        registration.Cancel(utcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!wasRegistered)
        {
            return;
        }

        var oldestWaitlisted = await dbContext.Set<EventRegistration>()
            .Where(r => r.EventId == eventId && r.Status == EventRegistrationStatus.Waitlisted)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (oldestWaitlisted is null)
        {
            return;
        }

        oldestWaitlisted.Promote();

        var @event = await dbContext.Set<Event>().AsNoTracking().FirstAsync(e => e.Id == eventId, cancellationToken);
        EventReminderScheduler.ScheduleFor(dbContext, oldestWaitlisted, @event.StartsAtUtc, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        var promotedUser = await userLookup.GetByIdAsync(oldestWaitlisted.UserId, cancellationToken);
        if (promotedUser is not null)
        {
            await notificationSender.SendAsync(
                NotificationType.EventRegistrationConfirmed,
                promotedUser.Email,
                new Dictionary<string, string> { ["eventId"] = eventId.ToString(), ["status"] = EventRegistrationStatus.Registered.ToString() },
                cancellationToken);
        }
    }
}
