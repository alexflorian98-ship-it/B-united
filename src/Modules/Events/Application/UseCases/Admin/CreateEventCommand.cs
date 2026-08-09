using BUnited.Modules.Events.Domain;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed record CreateEventRequest(
    string DefaultLanguage,
    string Title,
    string Description,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    string? MeetingUrl,
    int? Capacity);

public sealed record CreateEventCommand(
    string DefaultLanguage,
    string Title,
    string Description,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    string? MeetingUrl,
    int? Capacity,
    Guid CreatedBy);
