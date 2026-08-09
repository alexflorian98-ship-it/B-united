using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Events.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DefaultLanguage).IsRequired().HasMaxLength(10);
        builder.Property(e => e.DisplayTimezone).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LocationType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(300);
        builder.Property(e => e.MeetingUrl).HasMaxLength(2000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(e => e.StartsAtUtc).IsRequired();
        builder.Property(e => e.EndsAtUtc).IsRequired();
        builder.HasIndex(e => e.StartsAtUtc);
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
