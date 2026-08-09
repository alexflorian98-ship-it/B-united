using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>One client's one-time purchase of a single program. <see cref="Amount"/>/
/// <see cref="Currency"/> are snapshotted from the <see cref="ProgramPrice"/> at creation time so
/// a later price change never retroactively changes a historical purchase.
/// <code>
/// [*] -> Pending
/// Pending -> Succeeded: payment succeeds
/// Pending -> Failed: payment fails/declines
/// Succeeded -> Refunded: refund issued
/// Succeeded -> Chargeback: chargeback received
/// </code>
/// No expiration/period concept — a Succeeded purchase grants permanent access via
/// <see cref="ProgramEntitlement"/> until explicitly revoked (refund/chargeback).</summary>
public sealed class Purchase : IAuditableEntity
{
    private Purchase()
    {
    }

    /// <summary><paramref name="programTitleSnapshot"/> is optional (defaulted) purely so every
    /// existing call site compiles unchanged — omitting it is only ever correct when the caller
    /// genuinely has no title available (should not happen for a real checkout, since
    /// <c>CreateProgramPurchaseHandler</c> always resolves one via <c>IProgramLookup</c>).</summary>
    public static Purchase Create(Guid userId, Guid programId, Guid programOfferId, Guid programPriceId, decimal amount, string currency, string? programTitleSnapshot = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProgramId = programId,
            ProgramOfferId = programOfferId,
            ProgramPriceId = programPriceId,
            Amount = amount,
            Currency = currency,
            Status = PurchaseStatus.Pending,
            ProgramTitleSnapshot = programTitleSnapshot,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProgramId { get; private set; }

    public Guid ProgramOfferId { get; private set; }

    public Guid ProgramPriceId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    /// <summary>The program's title in its default language, captured once at purchase creation
    /// (docs/IMPLEMENTATION_PLAN.md Slice A5) — immutable afterwards, like <see cref="Amount"/>/
    /// <see cref="Currency"/>. Historical billing records must keep a readable label even if the
    /// program is later renamed, unpublished, or archived; <see langword="null"/> only for
    /// purchases created before this field existed (backfilled where a title could still be
    /// resolved) or in the rare case no title was resolvable at checkout time.</summary>
    public string? ProgramTitleSnapshot { get; private set; }

    public PurchaseStatus Status { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void MarkSucceeded(DateTime utcNow)
    {
        // Pending -> Succeeded is the normal path; Succeeded -> Succeeded is a legitimate no-op
        // (a racing duplicate delivery reaching the transition after the idempotency/out-of-order
        // guards already let it through once) so a retry never throws.
        if (Status is not (PurchaseStatus.Pending or PurchaseStatus.Succeeded))
        {
            throw new InvalidOperationException($"Cannot mark a purchase succeeded from status {Status}.");
        }

        Status = PurchaseStatus.Succeeded;
        CompletedAtUtc ??= utcNow;
    }

    public void MarkFailed()
    {
        if (Status != PurchaseStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark a purchase failed from status {Status}.");
        }

        Status = PurchaseStatus.Failed;
    }

    public void MarkRefunded()
    {
        if (Status != PurchaseStatus.Succeeded)
        {
            throw new InvalidOperationException($"Cannot refund a purchase from status {Status}.");
        }

        Status = PurchaseStatus.Refunded;
    }

    public void MarkChargedBack()
    {
        if (Status != PurchaseStatus.Succeeded)
        {
            throw new InvalidOperationException($"Cannot charge back a purchase from status {Status}.");
        }

        Status = PurchaseStatus.Chargeback;
    }
}
