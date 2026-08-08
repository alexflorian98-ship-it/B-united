using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Progress.Infrastructure.Persistence.Configurations;

public sealed class SectionProgressConfiguration : IEntityTypeConfiguration<SectionProgress>
{
    public void Configure(EntityTypeBuilder<SectionProgress> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => new { p.UserId, p.SectionId }).IsUnique();
    }
}
