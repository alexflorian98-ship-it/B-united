using BUnited.Modules.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Audit.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.TimestampUtc).IsRequired();

        // No FK to Identity's User: modules must not reference another module's Domain layer
        // (CLAUDE.md). ActorUserId is an opaque identifier only.
        builder.HasIndex(a => a.ActorUserId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
