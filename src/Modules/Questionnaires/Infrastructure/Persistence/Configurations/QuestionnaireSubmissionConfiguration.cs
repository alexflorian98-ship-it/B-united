using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionnaireSubmissionConfiguration : IEntityTypeConfiguration<QuestionnaireSubmission>
{
    public void Configure(EntityTypeBuilder<QuestionnaireSubmission> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.QuestionnaireId);
        builder.HasIndex(s => s.Status);
        builder.HasOne<Questionnaire>().WithMany().HasForeignKey(s => s.QuestionnaireId).OnDelete(DeleteBehavior.Restrict);
    }
}
