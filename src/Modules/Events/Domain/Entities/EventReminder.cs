using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Events.Domain.Entities;

/// <summary>docs/PROMPT.md §29-31 (P5.03). One row per (EventRegistrationId, Type), created
/// up front at registration time with its computed fire time — the reminder job (P5.08) only
/// has to poll for due, unsent rows, never recompute schedules. The unique index on
/// (EventRegistrationId, Type) plus the idempotent <see cref="MarkSent"/> makes duplicate job
/// runs a no-op, satisfying "idempotent, retryable".</summary>
public sealed class EventReminder : IAuditableEntity
{
    private EventReminder()
    {
    }

    public static EventReminder Create(Guid eventRegistrationId, Guid eventId, EventReminderType type, DateTime scheduledForUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventRegistrationId = eventRegistrationId,
            EventId = eventId,
            Type = type,
            ScheduledForUtc = scheduledForUtc,
        };

    public Guid Id { get; private set; }

    public Guid EventRegistrationId { get; private set; }

    public Guid EventId { get; private set; }

    public EventReminderType Type { get; private set; }

    public DateTime ScheduledForUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void MarkSent(DateTime utcNow) => SentAtUtc ??= utcNow;

    /// <summary>Recomputes the fire time after the event's schedule changes (P5.15.b). A no-op
    /// once the reminder has already been sent — a sent reminder is history, not a live
    /// schedule entry.</summary>
    public void Reschedule(DateTime scheduledForUtc)
    {
        if (SentAtUtc is not null)
        {
            return;
        }

        ScheduledForUtc = scheduledForUtc;
    }
}
