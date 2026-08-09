using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.CreatedAt).IsRequired();
        builder.Property(q => q.UpdatedAt).IsRequired();

        builder.HasIndex(q => q.ContentItemId);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(q => q.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
