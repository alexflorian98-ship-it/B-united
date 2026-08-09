using BUnited.Modules.Events.Domain;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed record UpdateEventScheduleRequest(
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    string? MeetingUrl,
    int? Capacity);

public sealed record UpdateEventScheduleCommand(
    Guid EventId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    string? MeetingUrl,
    int? Capacity,
    Guid UpdatedBy);
