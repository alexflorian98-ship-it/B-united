using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

/// <summary>§52 admin event list (Title, Date, Type, Registrations, Capacity, Status).</summary>
public sealed class ListEventsHandler(DbContext dbContext)
{
    public async Task<AdminEventListResult> HandleAsync(ListEventsQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var baseQuery = dbContext.Set<Event>().AsNoTracking().OrderByDescending(e => e.StartsAtUtc);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var eventIds = page.Select(e => e.Id).ToList();

        var titles = await dbContext.Set<EventTranslation>().AsNoTracking()
            .Where(t => eventIds.Contains(t.EventId))
            .ToListAsync(cancellationToken);

        var registeredCounts = await dbContext.Set<EventRegistration>().AsNoTracking()
            .Where(r => eventIds.Contains(r.EventId) && r.Status == EventRegistrationStatus.Registered)
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var items = page.Select(e =>
        {
            var translation = titles.FirstOrDefault(t => t.EventId == e.Id && t.Language == e.DefaultLanguage)
                ?? titles.FirstOrDefault(t => t.EventId == e.Id);
            var registeredCount = registeredCounts.FirstOrDefault(c => c.EventId == e.Id)?.Count ?? 0;

            return new AdminEventListItemDto(
                e.Id,
                translation?.Title ?? string.Empty,
                e.StartsAtUtc,
                e.DisplayTimezone,
                e.LocationType,
                registeredCount,
                e.Capacity,
                e.EffectiveStatus(utcNow).ToString());
        }).ToList();

        return new AdminEventListResult(items, totalCount, query.Page, query.PageSize);
    }
}
