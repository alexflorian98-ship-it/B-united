using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class QuizQuestionTranslationConfiguration : IEntityTypeConfiguration<QuizQuestionTranslation>
{
    public void Configure(EntityTypeBuilder<QuizQuestionTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Text).IsRequired().HasMaxLength(1000);

        builder.HasIndex(t => new { t.QuizQuestionId, t.Language }).IsUnique();
        builder.HasOne<QuizQuestion>().WithMany().HasForeignKey(t => t.QuizQuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
