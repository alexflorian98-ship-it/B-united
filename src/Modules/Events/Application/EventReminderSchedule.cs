using BUnited.Modules.Events.Domain;

namespace BUnited.Modules.Events.Application;

/// <summary>Computes the 24h/1h reminder fire times from an event's start time (docs/PROMPT.md
/// §29-31). Shared between registration-time scheduling and reschedule-on-edit so both use
/// identical arithmetic.</summary>
public static class EventReminderSchedule
{
    public static readonly IReadOnlyList<(EventReminderType Type, TimeSpan LeadTime)> Offsets =
    [
        (EventReminderType.TwentyFourHour, TimeSpan.FromHours(24)),
        (EventReminderType.OneHour, TimeSpan.FromHours(1)),
    ];

    public static DateTime FireTime(DateTime startsAtUtc, TimeSpan leadTime) => startsAtUtc - leadTime;
}
