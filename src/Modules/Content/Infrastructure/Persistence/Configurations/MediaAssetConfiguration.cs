using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Provider).IsRequired().HasMaxLength(50);
        builder.Property(m => m.ProviderAssetId).IsRequired().HasMaxLength(200);
        builder.Property(m => m.ProviderPlaybackId).HasMaxLength(200);
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(2000);
        builder.Property(m => m.ProcessingStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.Provider, m.ProviderAssetId }).IsUnique();
    }
}
