using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionnaireAnswerConfiguration : IEntityTypeConfiguration<QuestionnaireAnswer>
{
    public void Configure(EntityTypeBuilder<QuestionnaireAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Value).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => new { a.QuestionnaireSubmissionId, a.QuestionId }).IsUnique();
        builder.HasOne<QuestionnaireSubmission>().WithMany().HasForeignKey(a => a.QuestionnaireSubmissionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Question>().WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}
