using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Events.Infrastructure.Persistence.Configurations;

public sealed class EventTranslationConfiguration : IEntityTypeConfiguration<EventTranslation>
{
    public void Configure(EntityTypeBuilder<EventTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Description).IsRequired();

        builder.HasIndex(t => new { t.EventId, t.Language }).IsUnique();
        builder.HasOne<Event>().WithMany().HasForeignKey(t => t.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}
