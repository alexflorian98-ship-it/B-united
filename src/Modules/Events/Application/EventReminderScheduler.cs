using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application;

/// <summary>Creates the (up to) two <see cref="EventReminder"/> rows for a registration — used
/// both at registration time and at waitlist-promotion time, so both paths schedule identically.
/// An offset whose fire time has already passed is skipped entirely rather than created as an
/// immediately-due reminder — a "24h before" promise made 30 minutes before the event is not a
/// reminder worth sending.</summary>
public static class EventReminderScheduler
{
    public static void ScheduleFor(DbContext dbContext, EventRegistration registration, DateTime startsAtUtc, DateTime utcNow)
    {
        foreach (var (type, leadTime) in EventReminderSchedule.Offsets)
        {
            var fireTime = EventReminderSchedule.FireTime(startsAtUtc, leadTime);
            if (fireTime > utcNow)
            {
                dbContext.Set<EventReminder>().Add(EventReminder.Create(registration.Id, registration.EventId, type, fireTime));
            }
        }
    }
}
