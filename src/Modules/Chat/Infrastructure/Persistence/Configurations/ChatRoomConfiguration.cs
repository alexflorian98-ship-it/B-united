using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;

public sealed class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.Key).IsUnique();
        builder.HasIndex(r => r.ProgramId);
        builder.HasIndex(r => r.IsActive);
    }
}
