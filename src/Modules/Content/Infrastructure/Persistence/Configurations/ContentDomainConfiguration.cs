using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class ContentDomainConfiguration : IEntityTypeConfiguration<ContentDomain>
{
    public void Configure(EntityTypeBuilder<ContentDomain> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(d => d.Slug).IsUnique();
    }
}
