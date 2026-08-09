using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;

public sealed class MuteConfiguration : IEntityTypeConfiguration<Mute>
{
    public void Configure(EntityTypeBuilder<Mute> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.Property(m => m.ExpiresAtUtc).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.UserId, m.ExpiresAtUtc });
    }
}
