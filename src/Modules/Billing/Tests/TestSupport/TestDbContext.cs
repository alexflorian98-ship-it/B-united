using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(ProgramOfferConfiguration).Assembly])
{
    /// <summary><see cref="ProgramOffer"/>'s concurrency token maps to Postgres's inherent,
    /// always-populated <c>xmin</c> system column in production — Sqlite has no equivalent, so
    /// under test it would otherwise fail every insert with a NOT NULL violation (nothing
    /// generates a value for it). Reconfigured here to a plain, always-zero column so tests can
    /// exercise <see cref="ProgramOffer"/> at all — same pattern as Content's
    /// <c>Program</c>; this intentionally does not exercise real optimistic concurrency (that
    /// only matters, and only works, against the real Postgres provider).</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProgramOffer>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
    }

    public DbSet<ProgramOffer> ProgramOffers => Set<ProgramOffer>();

    public DbSet<ProgramPrice> ProgramPrices => Set<ProgramPrice>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<ProgramEntitlement> ProgramEntitlements => Set<ProgramEntitlement>();

    public DbSet<PaymentCustomer> PaymentCustomers => Set<PaymentCustomer>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
}
