using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class ProgramEntitlementConfiguration : IEntityTypeConfiguration<ProgramEntitlement>
{
    public void Configure(EntityTypeBuilder<ProgramEntitlement> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // The ADR-003 ownership invariant: one entitlement row per (user, program), ever —
        // extended/revoked/reactivated in place, never duplicated.
        builder.HasIndex(e => new { e.UserId, e.ProgramId }).IsUnique();
        builder.HasIndex(e => e.SourcePurchaseId);
    }
}
