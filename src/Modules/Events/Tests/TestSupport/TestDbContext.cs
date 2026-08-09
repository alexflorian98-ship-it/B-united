using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(EventConfiguration).Assembly])
{
    /// <summary><see cref="Event"/> and <see cref="EventRegistration"/>'s concurrency tokens map
    /// to Postgres's inherent, always-populated <c>xmin</c> system column in production — Sqlite
    /// has no equivalent, so under test it would otherwise fail every insert with a NOT NULL
    /// violation. Reconfigured here to a plain, always-zero column, same as Content's
    /// <c>Program</c> — this intentionally does not exercise real optimistic concurrency (that
    /// only matters, and only works, against the real Postgres provider).</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
        modelBuilder.Entity<EventRegistration>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
    }

    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventTranslation> EventTranslations => Set<EventTranslation>();

    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    public DbSet<EventReminder> EventReminders => Set<EventReminder>();

    public DbSet<EventProgram> EventPrograms => Set<EventProgram>();
}
