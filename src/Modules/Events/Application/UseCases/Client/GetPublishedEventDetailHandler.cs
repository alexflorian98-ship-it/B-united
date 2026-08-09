using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Client;

/// <summary>P5.11.b — event detail with registration status and capacity/waitlist info.</summary>
public sealed class GetPublishedEventDetailHandler(DbContext dbContext)
{
    public async Task<EventDetailDto> HandleAsync(Guid eventId, Guid userId, string language, CancellationToken cancellationToken)
    {
        var @event = await dbContext.Set<Event>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Status == EventStatus.Published, cancellationToken)
            ?? throw new NotFoundAppException("The specified event does not exist.");

        var translation = await dbContext.Set<EventTranslation>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.Language == language, cancellationToken)
            ?? await dbContext.Set<EventTranslation>().AsNoTracking()
                .FirstOrDefaultAsync(t => t.EventId == eventId && t.Language == @event.DefaultLanguage, cancellationToken);

        var registeredCount = await dbContext.Set<EventRegistration>().AsNoTracking()
            .CountAsync(r => r.EventId == eventId && r.Status == EventRegistrationStatus.Registered, cancellationToken);

        var waitlistedCount = await dbContext.Set<EventRegistration>().AsNoTracking()
            .CountAsync(r => r.EventId == eventId && r.Status == EventRegistrationStatus.Waitlisted, cancellationToken);

        var myRegistration = await dbContext.Set<EventRegistration>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != EventRegistrationStatus.Canceled, cancellationToken);

        return new EventDetailDto(
            @event.Id,
            translation?.Title ?? string.Empty,
            translation?.Description ?? string.Empty,
            @event.StartsAtUtc,
            @event.EndsAtUtc,
            @event.DisplayTimezone,
            @event.LocationType,
            @event.Location,
            @event.MeetingUrl,
            @event.Capacity,
            registeredCount,
            waitlistedCount,
            @event.EffectiveStatus(DateTime.UtcNow).ToString(),
            myRegistration?.Status.ToString());
    }
}
