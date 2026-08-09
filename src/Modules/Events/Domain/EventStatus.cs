namespace BUnited.Modules.Events.Domain;

/// <summary>docs/PROMPT.md §29-31. <c>Completed</c> is never persisted directly — it is derived
/// from <c>Published</c> plus <c>EndsAtUtc</c> having passed (see <c>Event.EffectiveStatus</c>),
/// the same "no background sweep job" trick used by Billing's <c>Entitlement.IsActiveAt</c>.</summary>
public enum EventStatus
{
    Draft,
    Published,
    Canceled,
    Completed,
}
