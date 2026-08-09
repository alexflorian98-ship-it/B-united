using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Events.Domain.Entities;

/// <summary>docs/PROMPT.md §29-31. One row per (EventId, UserId) — enforced by a unique index
/// (P5.02.a) — so a cancel-then-re-register flow reactivates the same row rather than inserting
/// a duplicate. <see cref="CreatedAt"/> is the FIFO ordering key for waitlist promotion ("promote
/// the oldest eligible waitlisted user").</summary>
public sealed class EventRegistration : IAuditableEntity
{
    private EventRegistration()
    {
    }

    public static EventRegistration Create(Guid eventId, Guid userId, EventRegistrationStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = status,
        };

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid UserId { get; private set; }

    public EventRegistrationStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CanceledAt { get; private set; }

    /// <summary>Reactivates a previously canceled registration (re-registration after a cancel) —
    /// otherwise the unique (EventId, UserId) index would force a second row.</summary>
    public void Reactivate(EventRegistrationStatus status)
    {
        if (Status != EventRegistrationStatus.Canceled)
        {
            throw new InvalidOperationException("Only a canceled registration can be reactivated.");
        }

        Status = status;
        CanceledAt = null;
    }

    public void Promote()
    {
        if (Status != EventRegistrationStatus.Waitlisted)
        {
            throw new InvalidOperationException("Only a waitlisted registration can be promoted.");
        }

        Status = EventRegistrationStatus.Registered;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status == EventRegistrationStatus.Canceled)
        {
            return;
        }

        Status = EventRegistrationStatus.Canceled;
        CanceledAt = utcNow;
    }
}
