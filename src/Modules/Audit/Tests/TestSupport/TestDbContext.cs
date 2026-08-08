using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Audit.Domain.Entities;
using BUnited.Modules.Audit.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Audit.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(AuditLogConfiguration).Assembly])
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
