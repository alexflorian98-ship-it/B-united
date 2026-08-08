using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasIndex(c => c.SectionId);
        builder.HasOne<Section>().WithMany().HasForeignKey(c => c.SectionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.MediaAssetId);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(c => c.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
