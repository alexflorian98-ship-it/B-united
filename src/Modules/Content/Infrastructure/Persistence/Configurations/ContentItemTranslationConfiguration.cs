using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class ContentItemTranslationConfiguration : IEntityTypeConfiguration<ContentItemTranslation>
{
    public void Configure(EntityTypeBuilder<ContentItemTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);

        builder.HasIndex(t => new { t.ContentItemId, t.Language }).IsUnique();
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(t => t.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
