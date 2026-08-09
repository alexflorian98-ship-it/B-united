using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.ProgramTitleSnapshot).HasMaxLength(300);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.ProgramId);
        builder.HasIndex(p => p.Status);

        // P3.38.b: prevents duplicate pending purchases for the same (user, program) — a repeat
        // checkout call reuses the existing pending row instead of racing a duplicate insert.
        builder.HasIndex(p => new { p.UserId, p.ProgramId })
            .IsUnique()
            .HasFilter("\"status\" = 'Pending'")
            .HasDatabaseName("ix_purchases_user_id_program_id_pending");
    }
}
