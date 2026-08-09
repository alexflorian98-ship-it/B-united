using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Client;

/// <summary>P5.13.a — dashboard "upcoming event" card (§41).</summary>
public sealed class GetMyUpcomingEventHandler(DbContext dbContext)
{
    public async Task<MyRegistrationDto?> HandleAsync(Guid userId, string language, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var registration = await dbContext.Set<EventRegistration>().AsNoTracking()
            .Where(r => r.UserId == userId && r.Status != EventRegistrationStatus.Canceled)
            .Join(dbContext.Set<Event>().AsNoTracking().Where(e => e.Status == EventStatus.Published && e.StartsAtUtc > utcNow),
                r => r.EventId,
                e => e.Id,
                (r, e) => new { Registration = r, Event = e })
            .OrderBy(x => x.Event.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (registration is null)
        {
            return null;
        }

        var translation = await dbContext.Set<EventTranslation>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.EventId == registration.Event.Id && t.Language == language, cancellationToken)
            ?? await dbContext.Set<EventTranslation>().AsNoTracking()
                .FirstOrDefaultAsync(t => t.EventId == registration.Event.Id && t.Language == registration.Event.DefaultLanguage, cancellationToken);

        return new MyRegistrationDto(
            registration.Event.Id,
            translation?.Title ?? string.Empty,
            registration.Event.StartsAtUtc,
            registration.Event.DisplayTimezone,
            registration.Registration.Status.ToString());
    }
}
