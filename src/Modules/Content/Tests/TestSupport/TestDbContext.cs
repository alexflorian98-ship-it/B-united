using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Content.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(ProgramConfiguration).Assembly])
{
    /// <summary><see cref="Program"/>'s concurrency token maps to Postgres's inherent, always-
    /// populated <c>xmin</c> system column in production — Sqlite has no equivalent, so under
    /// test it would otherwise fail every insert with a NOT NULL violation (nothing generates a
    /// value for it). Reconfigured here to a plain, always-zero column so tests can exercise
    /// <see cref="Program"/> at all; this intentionally does not exercise real optimistic
    /// concurrency (that only matters, and only works, against the real Postgres provider).</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Program>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
    }

    public DbSet<ContentDomain> ContentDomains => Set<ContentDomain>();

    public DbSet<Program> Programs => Set<Program>();

    public DbSet<ProgramTranslation> ProgramTranslations => Set<ProgramTranslation>();

    public DbSet<Section> Sections => Set<Section>();

    public DbSet<SectionTranslation> SectionTranslations => Set<SectionTranslation>();

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    public DbSet<ContentItemTranslation> ContentItemTranslations => Set<ContentItemTranslation>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
}
