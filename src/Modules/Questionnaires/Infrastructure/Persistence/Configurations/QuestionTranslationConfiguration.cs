using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionTranslationConfiguration : IEntityTypeConfiguration<QuestionTranslation>
{
    public void Configure(EntityTypeBuilder<QuestionTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Text).IsRequired().HasMaxLength(1000);
        builder.Property(t => t.HelpText).HasMaxLength(1000);

        builder.HasIndex(t => new { t.QuestionId, t.Language }).IsUnique();
        builder.HasOne<Question>().WithMany().HasForeignKey(t => t.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
