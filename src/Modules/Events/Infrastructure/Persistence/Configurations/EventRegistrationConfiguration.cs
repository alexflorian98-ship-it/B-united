using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Events.Infrastructure.Persistence.Configurations;

public sealed class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        // P5.02.a: one registration row per (EventId, UserId) — re-registration reactivates it.
        builder.HasIndex(r => new { r.EventId, r.UserId }).IsUnique();
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.EventId, r.Status, r.CreatedAt });

        builder.HasOne<Event>().WithMany().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
