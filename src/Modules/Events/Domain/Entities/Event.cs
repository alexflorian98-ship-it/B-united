using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Events.Domain.Entities;

/// <summary>docs/PROMPT.md §29-31. All scheduling fields are stored in UTC; <see cref="DisplayTimezone"/>
/// (an IANA id, e.g. "Europe/Bucharest") is retained separately purely for rendering, per
/// docs/DEVELOPMENT_INSTRUCTIONS.md §5 ("Event records MUST retain their display timezone
/// separately"). <see cref="Status"/> is admin-controlled (Draft/Published/Canceled) — the
/// <c>Completed</c> member is never persisted, only computed by <see cref="EffectiveStatus"/>,
/// avoiding the need for a background sweep job (mirrors Billing's Entitlement.IsActiveAt).</summary>
public sealed class Event : IAuditableEntity
{
    private Event()
    {
    }

    public static Event Create(
        string defaultLanguage,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        string displayTimezone,
        EventLocationType locationType,
        string? location,
        string? meetingUrl,
        int? capacity,
        Guid? createdBy)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            throw new InvalidOperationException("EndsAtUtc must be after StartsAtUtc.");
        }

        return new Event
        {
            Id = Guid.NewGuid(),
            DefaultLanguage = defaultLanguage,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            DisplayTimezone = displayTimezone,
            LocationType = locationType,
            Location = location,
            MeetingUrl = meetingUrl,
            Capacity = capacity,
            Status = EventStatus.Draft,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };
    }

    public Guid Id { get; private set; }

    public string DefaultLanguage { get; private set; } = string.Empty;

    public DateTime StartsAtUtc { get; private set; }

    public DateTime EndsAtUtc { get; private set; }

    public string DisplayTimezone { get; private set; } = string.Empty;

    public EventLocationType LocationType { get; private set; }

    public string? Location { get; private set; }

    public string? MeetingUrl { get; private set; }

    public int? Capacity { get; private set; }

    public EventStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    /// <summary>The status to actually show/act on — folds in the derived <c>Completed</c> state
    /// without ever persisting it.</summary>
    public EventStatus EffectiveStatus(DateTime utcNow) =>
        Status == EventStatus.Published && EndsAtUtc <= utcNow ? EventStatus.Completed : Status;

    public void UpdateSchedule(DateTime startsAtUtc, DateTime endsAtUtc, string displayTimezone, Guid updatedBy)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            throw new InvalidOperationException("EndsAtUtc must be after StartsAtUtc.");
        }

        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        DisplayTimezone = displayTimezone;
        UpdatedBy = updatedBy;
    }

    public void UpdateLocation(EventLocationType locationType, string? location, string? meetingUrl, Guid updatedBy)
    {
        LocationType = locationType;
        Location = location;
        MeetingUrl = meetingUrl;
        UpdatedBy = updatedBy;
    }

    public void UpdateCapacity(int? capacity, Guid updatedBy)
    {
        Capacity = capacity;
        UpdatedBy = updatedBy;
    }

    public void Publish(DateTime utcNow, Guid updatedBy)
    {
        if (Status == EventStatus.Canceled)
        {
            throw new InvalidOperationException("A canceled event cannot be published.");
        }

        Status = EventStatus.Published;
        PublishedAt ??= utcNow;
        UpdatedBy = updatedBy;
    }

    public void Cancel(Guid updatedBy)
    {
        if (Status == EventStatus.Canceled)
        {
            return;
        }

        Status = EventStatus.Canceled;
        UpdatedBy = updatedBy;
    }
}
