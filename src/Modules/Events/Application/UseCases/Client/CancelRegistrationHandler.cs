using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Client;

/// <summary>P5.06.b: canceling a Registered (seat-holding) registration promotes the oldest
/// eligible waitlisted user. Canceling a Waitlisted registration is a plain no-op beyond the
/// cancellation itself — nobody needs to move up because no seat was freed.</summary>
public sealed class CancelRegistrationHandler(DbContext dbContext)
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
    }
}
