namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>Money is always <c>decimal</c> with an explicit currency (docs/PROMPT.md §63) —
/// never <c>float</c>/<c>double</c>. Immutable once created: a price change creates a new row
/// rather than mutating one a <see cref="Purchase"/> may already reference (P3.35/36), so a
/// historical purchase's snapshotted amount is never affected by a later price change.</summary>
public sealed class ProgramPrice
{
    private ProgramPrice()
    {
    }

    public static ProgramPrice Create(Guid programOfferId, decimal amount, string currency, DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProgramOfferId = programOfferId,
            Amount = amount,
            Currency = currency,
            CreatedAtUtc = createdAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid ProgramOfferId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }
}
