using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Migrations.Seed;

/// <summary>Idempotent seed of a single demo <see cref="ProgramOffer"/>+<see cref="ProgramPrice"/>
/// against the first seeded Content program (by <see cref="Program.SortOrder"/>, falling back to
/// creation order), so checkout has something to sell without requiring an admin-authored offer
/// first (ADR-003's per-program purchase model replaces the old global "Standard" plan).
/// Guarded by an existing-active-offer check for that program — same idempotency guarantee as
/// <see cref="ContentSeeder"/>, just checked differently (no well-known fixed ID here).</summary>
public static class ProgramOfferSeeder
{
    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var program = await context.Programs
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (program is null)
        {
            // ContentSeeder only seeds content domains, not programs themselves — programs are
            // admin-authored. Nothing to sell yet; a later run (after an admin creates a
            // program) will pick this up.
            return;
        }

        var hasActiveOffer = await context.Set<ProgramOffer>()
            .AnyAsync(o => o.ProgramId == program.Id && o.Status == ProgramOfferStatus.Active, cancellationToken);
        if (hasActiveOffer)
        {
            return;
        }

        var offer = ProgramOffer.Create(program.Id);
        offer.Activate();
        context.Set<ProgramOffer>().Add(offer);

        context.Set<ProgramPrice>().Add(ProgramPrice.Create(offer.Id, 299.00m, "RON", DateTime.UtcNow));

        await context.SaveChangesAsync(cancellationToken);
    }
}
