using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

/// <summary>§52 admin event detail: registered users, waitlist, and reminder status.</summary>
public sealed class GetEventDetailAdminHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<AdminEventDetailDto> HandleAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await dbContext.Set<Event>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new NotFoundAppException("The specified event does not exist.");

        var translations = await dbContext.Set<EventTranslation>().AsNoTracking()
            .Where(t => t.EventId == eventId)
            .Select(t => new EventTranslationDto(t.Language, t.Title, t.Description))
            .ToListAsync(cancellationToken);

        var registrations = await dbContext.Set<EventRegistration>().AsNoTracking()
            .Where(r => r.EventId == eventId && r.Status != EventRegistrationStatus.Canceled)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var reminders = await dbContext.Set<EventReminder>().AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.ScheduledForUtc)
            .ToListAsync(cancellationToken);

        var programIds = await dbContext.Set<EventProgram>().AsNoTracking()
            .Where(ep => ep.EventId == eventId)
            .Select(ep => ep.ProgramId)
            .ToListAsync(cancellationToken);

        var userIds = registrations.Select(r => r.UserId).Distinct().ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        var registrationById = registrations.ToDictionary(r => r.Id);

        var registrationDtos = registrations
            .Where(r => r.Status == EventRegistrationStatus.Registered)
            .Select(r => ToSummary(r, users))
            .ToList();

        var waitlistDtos = registrations
            .Where(r => r.Status == EventRegistrationStatus.Waitlisted)
            .Select(r => ToSummary(r, users))
            .ToList();

        var reminderDtos = reminders.Select(r =>
        {
            var email = registrationById.TryGetValue(r.EventRegistrationId, out var registration) && users.TryGetValue(registration.UserId, out var user)
                ? user.Email
                : null;
            return new EventReminderSummaryDto(r.EventRegistrationId, email, r.Type.ToString(), r.ScheduledForUtc, r.SentAtUtc);
        }).ToList();

        return new AdminEventDetailDto(
            @event.Id,
            @event.StartsAtUtc,
            @event.EndsAtUtc,
            @event.DisplayTimezone,
            @event.LocationType,
            @event.Location,
            @event.MeetingUrl,
            @event.Capacity,
            @event.EffectiveStatus(DateTime.UtcNow).ToString(),
            @event.DefaultLanguage,
            translations,
            registrationDtos,
            waitlistDtos,
            reminderDtos,
            programIds);
    }

    private static EventRegistrationSummaryDto ToSummary(EventRegistration registration, IReadOnlyDictionary<Guid, UserSummary> users) =>
        new(
            registration.Id,
            registration.UserId,
            users.TryGetValue(registration.UserId, out var user) ? user.Email : null,
            registration.Status.ToString(),
            registration.CreatedAt);
}
