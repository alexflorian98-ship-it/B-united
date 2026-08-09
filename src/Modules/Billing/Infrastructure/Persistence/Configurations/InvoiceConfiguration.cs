using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasIndex(i => i.PurchaseId);
        builder.HasIndex(i => i.PaymentId);
        builder.HasOne<Purchase>().WithMany().HasForeignKey(i => i.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Payment>().WithMany().HasForeignKey(i => i.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}
