using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class QuizOptionTranslationConfiguration : IEntityTypeConfiguration<QuizOptionTranslation>
{
    public void Configure(EntityTypeBuilder<QuizOptionTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Label).IsRequired().HasMaxLength(300);

        builder.HasIndex(t => new { t.QuizOptionId, t.Language }).IsUnique();
        builder.HasOne<QuizOption>().WithMany().HasForeignKey(t => t.QuizOptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
