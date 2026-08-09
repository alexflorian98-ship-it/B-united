using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.ProviderPaymentId).IsRequired().HasMaxLength(200);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.PurchaseId);
        builder.HasIndex(p => p.ProviderPaymentId).IsUnique();
        builder.HasOne<Purchase>().WithMany().HasForeignKey(p => p.PurchaseId).OnDelete(DeleteBehavior.Cascade);
    }
}
