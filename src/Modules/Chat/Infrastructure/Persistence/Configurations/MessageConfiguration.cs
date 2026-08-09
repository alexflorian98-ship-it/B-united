using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RoomId).IsRequired();
        builder.Property(m => m.Body).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.RoomId, m.CreatedAt });
        builder.HasIndex(m => m.UserId);
        builder.HasOne<ChatRoom>().WithMany().HasForeignKey(m => m.RoomId).OnDelete(DeleteBehavior.Restrict);
    }
}
