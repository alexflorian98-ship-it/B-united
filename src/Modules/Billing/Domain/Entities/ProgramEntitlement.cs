using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>The ADR-003 ownership record: one row per (<see cref="UserId"/>, <see cref="ProgramId"/>)
/// — enforced by a unique database index, never duplicated — granting permanent access once a
/// <see cref="Purchase"/> succeeds. Deliberately has <b>no expiration field at all</b>: unlike the
/// old global subscription-backed entitlement, access here never lapses over time; it is only
/// ever flipped by an explicit <see cref="Revoke"/> (refund/chargeback), matching
/// "Subscription expiration MUST NEVER delete the account or historical user data"
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §5) — revocation is a status flip, never a delete.</summary>
public sealed class ProgramEntitlement : IAuditableEntity
{
    private ProgramEntitlement()
    {
    }

    public static ProgramEntitlement Grant(Guid userId, Guid programId, Guid sourcePurchaseId, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProgramId = programId,
            GrantedAtUtc = utcNow,
            Status = ProgramEntitlementStatus.Active,
            SourcePurchaseId = sourcePurchaseId,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProgramId { get; private set; }

    public DateTime GrantedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public ProgramEntitlementStatus Status { get; private set; }

    /// <summary>The <see cref="Purchase"/> that most recently granted/re-granted this
    /// entitlement — updated on <see cref="Reactivate"/>, so it always reflects the purchase
    /// currently backing access.</summary>
    public Guid SourcePurchaseId { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive => Status == ProgramEntitlementStatus.Active;

    /// <summary><paramref name="reason"/> is accepted for call-site clarity and audit-log
    /// metadata (e.g. "refund"/"chargeback") — this entity itself has no field for it, per the
    /// approved schema (no expiration/reason columns beyond what's listed above).</summary>
    public void Revoke(string reason, DateTime utcNow)
    {
        _ = reason;

        if (Status == ProgramEntitlementStatus.Revoked)
        {
            // Idempotent: revoking an already-revoked entitlement is a safe no-op, matching the
            // old Entitlement.Revoke() behavior.
            return;
        }

        Status = ProgramEntitlementStatus.Revoked;
        RevokedAtUtc = utcNow;
    }

    /// <summary>Re-grants access on the same row (never a new row — the unique
    /// (UserId, ProgramId) index forbids it) when a user re-purchases a program whose
    /// entitlement was previously revoked.</summary>
    public void Reactivate(Guid sourcePurchaseId, DateTime utcNow)
    {
        Status = ProgramEntitlementStatus.Active;
        RevokedAtUtc = null;
        SourcePurchaseId = sourcePurchaseId;
        GrantedAtUtc = utcNow;
    }
}
