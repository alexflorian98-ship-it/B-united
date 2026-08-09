using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class UpdateEventScheduleHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpdateEventScheduleCommand command, CancellationToken cancellationToken)
    {
        var @event = await dbContext.Set<Event>().FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundAppException("The specified event does not exist.");

        @event.UpdateSchedule(command.StartsAtUtc, command.EndsAtUtc, command.DisplayTimezone, command.UpdatedBy);
        @event.UpdateLocation(command.LocationType, command.Location, command.MeetingUrl, command.UpdatedBy);
        @event.UpdateCapacity(command.Capacity, command.UpdatedBy);

        var pendingReminders = await dbContext.Set<EventReminder>()
            .Where(r => r.EventId == command.EventId && r.SentAtUtc == null)
            .ToListAsync(cancellationToken);

        var offsetsByType = EventReminderSchedule.Offsets.ToDictionary(o => o.Type, o => o.LeadTime);
        foreach (var reminder in pendingReminders)
        {
            reminder.Reschedule(EventReminderSchedule.FireTime(command.StartsAtUtc, offsetsByType[reminder.Type]));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
