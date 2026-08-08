using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionnaireTranslationConfiguration : IEntityTypeConfiguration<QuestionnaireTranslation>
{
    public void Configure(EntityTypeBuilder<QuestionnaireTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Description).IsRequired();

        builder.HasIndex(t => new { t.QuestionnaireId, t.Language }).IsUnique();
        builder.HasOne<Questionnaire>().WithMany().HasForeignKey(t => t.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
    }
}
