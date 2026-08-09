using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;

public sealed class ChatReadStateConfiguration : IEntityTypeConfiguration<ChatReadState>
{
    public void Configure(EntityTypeBuilder<ChatReadState> builder)
    {
        builder.HasKey(r => new { r.UserId, r.RoomId });

        builder.Property(r => r.LastReadAtUtc).IsRequired();

        builder.HasOne<ChatRoom>().WithMany().HasForeignKey(r => r.RoomId).OnDelete(DeleteBehavior.Cascade);
    }
}
