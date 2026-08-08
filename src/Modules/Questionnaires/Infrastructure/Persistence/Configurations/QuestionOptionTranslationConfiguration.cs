using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionOptionTranslationConfiguration : IEntityTypeConfiguration<QuestionOptionTranslation>
{
    public void Configure(EntityTypeBuilder<QuestionOptionTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Label).IsRequired().HasMaxLength(300);

        builder.HasIndex(t => new { t.QuestionOptionId, t.Language }).IsUnique();
        builder.HasOne<QuestionOption>().WithMany().HasForeignKey(t => t.QuestionOptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
