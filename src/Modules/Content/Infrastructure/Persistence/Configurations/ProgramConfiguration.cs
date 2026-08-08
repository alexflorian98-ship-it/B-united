using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class ProgramConfiguration : IEntityTypeConfiguration<Program>
{
    public void Configure(EntityTypeBuilder<Program> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.DefaultLanguage).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.DomainId);
        builder.HasOne<ContentDomain>().WithMany().HasForeignKey(p => p.DomainId).OnDelete(DeleteBehavior.Restrict);

        // Postgres's native row-version column — no CLR-visible token property needed.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
