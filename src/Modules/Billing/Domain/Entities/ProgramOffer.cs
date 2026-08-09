using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>One sellable offer for a single Content program (opaque <see cref="ProgramId"/> —
/// no FK, Billing must never reference Content's Domain/Infrastructure, CLAUDE.md). Optimistic
/// concurrency uses Postgres's native <c>xmin</c> system column, mirroring
/// <c>Content.Program</c>'s style (configured in <c>ProgramOfferConfiguration</c>).</summary>
public sealed class ProgramOffer : IAuditableEntity
{
    private ProgramOffer()
    {
    }

    public static ProgramOffer Create(Guid programId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProgramId = programId,
            Status = ProgramOfferStatus.Draft,
        };

    public Guid Id { get; private set; }

    public Guid ProgramId { get; private set; }

    public ProgramOfferStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Activate() => Status = ProgramOfferStatus.Active;

    public void Deactivate() => Status = ProgramOfferStatus.Inactive;
}
