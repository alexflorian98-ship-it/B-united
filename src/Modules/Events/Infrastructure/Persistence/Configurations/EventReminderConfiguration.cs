using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Events.Infrastructure.Persistence.Configurations;

public sealed class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
    public void Configure(EntityTypeBuilder<EventReminder> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.ScheduledForUtc).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        // P5.19.a: the idempotency guard for job re-runs — one row per (registration, type).
        builder.HasIndex(r => new { r.EventRegistrationId, r.Type }).IsUnique();
        builder.HasIndex(r => new { r.ScheduledForUtc, r.SentAtUtc });

        builder.HasOne<EventRegistration>().WithMany().HasForeignKey(r => r.EventRegistrationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Event>().WithMany().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}
