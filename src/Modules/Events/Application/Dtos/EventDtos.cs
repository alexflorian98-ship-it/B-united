using BUnited.Modules.Events.Domain;

namespace BUnited.Modules.Events.Application.Dtos;

public sealed record EventSummaryDto(
    Guid Id,
    string Title,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    int? Capacity,
    int RegisteredCount,
    string Status,
    string? MyRegistrationStatus);

public sealed record EventDetailDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    string? MeetingUrl,
    int? Capacity,
    int RegisteredCount,
    int WaitlistedCount,
    string Status,
    string? MyRegistrationStatus);

public sealed record EventTranslationDto(string Language, string Title, string Description);

public sealed record AdminEventListItemDto(
    Guid Id,
    string Title,
    DateTime StartsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    int RegisteredCount,
    int? Capacity,
    string Status);

public sealed record AdminEventListResult(IReadOnlyList<AdminEventListItemDto> Items, int TotalCount, int Page, int PageSize);

public sealed record AdminEventDetailDto(
    Guid Id,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string DisplayTimezone,
    EventLocationType LocationType,
    string? Location,
    string? MeetingUrl,
    int? Capacity,
    string Status,
    string DefaultLanguage,
    IReadOnlyList<EventTranslationDto> Translations,
    IReadOnlyList<EventRegistrationSummaryDto> Registrations,
    IReadOnlyList<EventRegistrationSummaryDto> Waitlist,
    IReadOnlyList<EventReminderSummaryDto> Reminders,
    IReadOnlyList<Guid> ProgramIds);

public sealed record EventRegistrationSummaryDto(Guid RegistrationId, Guid UserId, string? Email, string Status, DateTime RegisteredAt);

public sealed record EventReminderSummaryDto(Guid RegistrationId, string? Email, string Type, DateTime ScheduledForUtc, DateTime? SentAtUtc);

public sealed record MyRegistrationDto(Guid EventId, string EventTitle, DateTime StartsAtUtc, string DisplayTimezone, string Status);
