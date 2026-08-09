using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Notifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Client;

/// <summary>P5.05-P5.07: registration closes at event start and assigns Registered vs Waitlisted
/// based on capacity (P5.06.a). Capacity-race safety (P5.06.c) is enforced with
/// <c>SELECT ... FOR UPDATE</c> on the event row when running against PostgreSQL — the Sqlite
/// test harness used by unit tests has no meaningful concurrent-writer story, so the lock is
/// skipped there and the race itself is instead covered by a live concurrent-request integration
/// check against the real database (see HANDOVER.md).
///
/// docs/TASKS.md P3.43.b: an event with zero <see cref="EventProgram"/> rows is
/// public-to-all-authenticated (today's original behavior, unchanged). An event with one or more
/// associated programs requires <see cref="IProgramAccessContext.HasProgramAccessAsync"/> to be
/// true for at least one of them, throwing <see cref="ProgramAccessErrorCodes.ProgramAccessRequired"/>
/// otherwise.</summary>
public sealed class RegisterForEventHandler(DbContext dbContext, INotificationSender notificationSender, IProgramAccessContext programAccessContext)
{
    public async Task<EventRegistrationStatus> HandleAsync(Guid eventId, Guid userId, string recipientEmail, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<EventRegistration>()
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken);

        if (existing is not null && existing.Status != EventRegistrationStatus.Canceled)
        {
            throw new BusinessRuleAppException("EVENT_ALREADY_REGISTERED", "errors.event.alreadyRegistered", "You are already registered for this event.");
        }

        var associatedProgramIds = await dbContext.Set<EventProgram>().AsNoTracking()
            .Where(ep => ep.EventId == eventId)
            .Select(ep => ep.ProgramId)
            .ToListAsync(cancellationToken);

        if (associatedProgramIds.Count > 0)
        {
            var hasAccessToAny = false;
            foreach (var programId in associatedProgramIds)
            {
                if (await programAccessContext.HasProgramAccessAsync(userId, programId, cancellationToken))
                {
                    hasAccessToAny = true;
                    break;
                }
            }

            if (!hasAccessToAny)
            {
                throw new BusinessRuleAppException(
                    ProgramAccessErrorCodes.ProgramAccessRequired,
                    "errors.programAccessRequired",
                    "Access to this program is required.");
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Providers other than PostgreSQL (the Sqlite test harness) have no meaningful
        // concurrent-writer story here — the lock is a PostgreSQL-only refinement, not a
        // correctness requirement for the sequential logic below. The lock is acquired as a
        // throwaway, unmapped command (not materialized into an Event) so it holds for the rest
        // of the transaction without EF trying to project system columns through it.
        var isPostgres = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isPostgres)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM events WHERE id = {eventId} FOR UPDATE", cancellationToken);
        }

        var @event = await dbContext.Set<Event>().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (@event is null)
        {
            throw new NotFoundAppException("The specified event does not exist.");
        }

        var utcNow = DateTime.UtcNow;
        if (@event.Status != EventStatus.Published)
        {
            throw new BusinessRuleAppException("EVENT_NOT_OPEN", "errors.event.notOpen", "This event is not open for registration.");
        }

        if (@event.StartsAtUtc <= utcNow)
        {
            throw new BusinessRuleAppException("EVENT_REGISTRATION_CLOSED", "errors.event.registrationClosed", "Registration closes when the event begins.");
        }

        var registeredCount = await dbContext.Set<EventRegistration>()
            .CountAsync(r => r.EventId == eventId && r.Status == EventRegistrationStatus.Registered, cancellationToken);

        var status = @event.Capacity is null || registeredCount < @event.Capacity
            ? EventRegistrationStatus.Registered
            : EventRegistrationStatus.Waitlisted;

        EventRegistration registration;
        if (existing is null)
        {
            registration = EventRegistration.Create(eventId, userId, status);
            dbContext.Set<EventRegistration>().Add(registration);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            existing.Reactivate(status);
            registration = existing;

            // A reactivated registration reuses the same Id — clear any leftover reminder rows
            // from its previous life before scheduling fresh ones, so the unique
            // (EventRegistrationId, Type) index doesn't collide.
            var staleReminders = await dbContext.Set<EventReminder>()
                .Where(r => r.EventRegistrationId == registration.Id)
                .ToListAsync(cancellationToken);
            dbContext.Set<EventReminder>().RemoveRange(staleReminders);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (status == EventRegistrationStatus.Registered)
        {
            EventReminderScheduler.ScheduleFor(dbContext, registration, @event.StartsAtUtc, utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        await notificationSender.SendAsync(
            NotificationType.EventRegistrationConfirmed,
            recipientEmail,
            new Dictionary<string, string> { ["eventId"] = eventId.ToString(), ["status"] = status.ToString() },
            cancellationToken);

        return status;
    }
}
