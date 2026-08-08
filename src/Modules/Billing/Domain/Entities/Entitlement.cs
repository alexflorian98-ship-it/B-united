using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>docs/PROMPT.md §15: a generic shape, but V1 uses exactly one
/// <see cref="Type"/> — <see cref="PlatformAccessType"/>. <see cref="ValidUntil"/> is the single
/// source of truth for "does access lapse over time" — set to the correct cutoff (grace-period
/// end for PastDue, period end for Canceled, <see langword="null"/> for indefinite Active/
/// Trialing) whenever the owning <see cref="Subscription"/> transitions, so a live date
/// comparison alone determines access without needing a background sweep job to eagerly revoke
/// anything (no job-scheduling infrastructure exists in this codebase yet).</summary>
public sealed class Entitlement : IAuditableEntity
{
    public const string PlatformAccessType = "PlatformAccess";

    private Entitlement()
    {
    }

    public static Entitlement Grant(Guid userId, string type, DateTime validFromUtc, DateTime? validUntilUtc, Guid sourceId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            ValidFromUtc = validFromUtc,
            ValidUntilUtc = validUntilUtc,
            Status = EntitlementStatus.Active,
            SourceType = nameof(Subscription),
            SourceId = sourceId,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public DateTime ValidFromUtc { get; private set; }

    public DateTime? ValidUntilUtc { get; private set; }

    public EntitlementStatus Status { get; private set; }

    public string SourceType { get; private set; } = string.Empty;

    public Guid SourceId { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Extend(DateTime? validUntilUtc)
    {
        Status = EntitlementStatus.Active;
        ValidUntilUtc = validUntilUtc;
    }

    public void Revoke() => Status = EntitlementStatus.Revoked;

    public bool IsActiveAt(DateTime utcNow) =>
        Status == EntitlementStatus.Active && (ValidUntilUtc is null || utcNow <= ValidUntilUtc);
}
