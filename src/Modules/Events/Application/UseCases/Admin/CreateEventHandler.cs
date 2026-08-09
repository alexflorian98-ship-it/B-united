using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class CreateEventHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var @event = Event.Create(
            command.DefaultLanguage,
            command.StartsAtUtc,
            command.EndsAtUtc,
            command.DisplayTimezone,
            command.LocationType,
            command.Location,
            command.MeetingUrl,
            command.Capacity,
            command.CreatedBy);

        dbContext.Set<Event>().Add(@event);

        var translation = EventTranslation.Create(@event.Id, command.DefaultLanguage, command.Title, command.Description);
        dbContext.Set<EventTranslation>().Add(translation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
