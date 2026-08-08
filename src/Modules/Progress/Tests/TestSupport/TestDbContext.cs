using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Progress.Domain.Entities;
using BUnited.Modules.Progress.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(ContentProgressConfiguration).Assembly])
{
    public DbSet<ContentProgress> ContentProgressEntries => Set<ContentProgress>();

    public DbSet<SectionProgress> SectionProgressEntries => Set<SectionProgress>();
}
