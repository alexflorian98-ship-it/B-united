using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.MessageId);
        builder.HasIndex(r => r.Status);
        builder.HasOne<Message>().WithMany().HasForeignKey(r => r.MessageId).OnDelete(DeleteBehavior.Restrict);
    }
}
