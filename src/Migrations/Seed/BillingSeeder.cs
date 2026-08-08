using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Migrations.Seed;

/// <summary>Idempotent seed of a single demo "Standard" plan with a monthly price, so checkout
/// (P3.06) has something to sell without requiring an admin-authored plan first. Guarded by a
/// name check (Plan has no well-known fixed ID the way content domains do) — same idempotency
/// guarantee as <see cref="ContentSeeder"/>, just checked differently.</summary>
public static class BillingSeeder
{
    private const string StandardPlanName = "Standard";

    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var exists = await context.Plans.AnyAsync(p => p.Name == StandardPlanName, cancellationToken);
        if (exists)
        {
            return;
        }

        var plan = Plan.Create(StandardPlanName, "Full access to all programs, community, and expert guidance.");
        context.Plans.Add(plan);

        context.PlanPrices.Add(PlanPrice.Create(plan.Id, 99.00m, "RON", BillingInterval.Monthly));

        await context.SaveChangesAsync(cancellationToken);
    }
}
