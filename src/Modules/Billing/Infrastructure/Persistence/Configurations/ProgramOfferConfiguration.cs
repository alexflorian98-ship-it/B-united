using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class ProgramOfferConfiguration : IEntityTypeConfiguration<ProgramOffer>
{
    public void Configure(EntityTypeBuilder<ProgramOffer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        // At most one active offer per program (P3.35) — no FK to Content, ProgramId is an
        // opaque Guid (CLAUDE.md: never reference another module's Domain/Infrastructure).
        builder.HasIndex(o => o.ProgramId)
            .IsUnique()
            .HasFilter("\"status\" = 'Active'")
            .HasDatabaseName("ix_program_offers_program_id_active");

        // Postgres's native row-version column — no CLR-visible token property needed, mirrors
        // Content.Program's style.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
