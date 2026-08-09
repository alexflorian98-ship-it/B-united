using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class ProgramPriceConfiguration : IEntityTypeConfiguration<ProgramPrice>
{
    public void Configure(EntityTypeBuilder<ProgramPrice> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasIndex(p => p.ProgramOfferId);
        builder.HasOne<ProgramOffer>().WithMany().HasForeignKey(p => p.ProgramOfferId).OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint("ck_program_prices_amount_positive", "\"amount\" > 0"));
    }
}
