using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class SectionTranslationConfiguration : IEntityTypeConfiguration<SectionTranslation>
{
    public void Configure(EntityTypeBuilder<SectionTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Description).IsRequired();

        builder.HasIndex(t => new { t.SectionId, t.Language }).IsUnique();
        builder.HasOne<Section>().WithMany().HasForeignKey(t => t.SectionId).OnDelete(DeleteBehavior.Cascade);
    }
}
