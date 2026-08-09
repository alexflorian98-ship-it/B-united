using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Client;

/// <summary>§41-44 events listing with upcoming/past filter (P5.11.a).</summary>
public sealed class ListPublishedEventsHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<EventSummaryDto>> HandleAsync(ListPublishedEventsQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var eventsQuery = dbContext.Set<Event>().AsNoTracking().Where(e => e.Status == EventStatus.Published);
        eventsQuery = query.IncludePast
            ? eventsQuery.Where(e => e.EndsAtUtc <= utcNow)
            : eventsQuery.Where(e => e.EndsAtUtc > utcNow);

        var events = await eventsQuery.OrderBy(e => e.StartsAtUtc).ToListAsync(cancellationToken);
        var eventIds = events.Select(e => e.Id).ToList();

        var translations = await dbContext.Set<EventTranslation>().AsNoTracking()
            .Where(t => eventIds.Contains(t.EventId) && (t.Language == query.Language || t.Language == "ro"))
            .ToListAsync(cancellationToken);

        var registeredCounts = await dbContext.Set<EventRegistration>().AsNoTracking()
            .Where(r => eventIds.Contains(r.EventId) && r.Status == EventRegistrationStatus.Registered)
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var myRegistrations = await dbContext.Set<EventRegistration>().AsNoTracking()
            .Where(r => eventIds.Contains(r.EventId) && r.UserId == query.UserId && r.Status != EventRegistrationStatus.Canceled)
            .ToListAsync(cancellationToken);

        return events.Select(e =>
        {
            var translation = translations.FirstOrDefault(t => t.EventId == e.Id && t.Language == query.Language)
                ?? translations.FirstOrDefault(t => t.EventId == e.Id && t.Language == e.DefaultLanguage);
            var registeredCount = registeredCounts.FirstOrDefault(c => c.EventId == e.Id)?.Count ?? 0;
            var myRegistration = myRegistrations.FirstOrDefault(r => r.EventId == e.Id);

            return new EventSummaryDto(
                e.Id,
                translation?.Title ?? string.Empty,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.DisplayTimezone,
                e.LocationType,
                e.Location,
                e.Capacity,
                registeredCount,
                e.EffectiveStatus(utcNow).ToString(),
                myRegistration?.Status.ToString());
        }).ToList();
    }
}
