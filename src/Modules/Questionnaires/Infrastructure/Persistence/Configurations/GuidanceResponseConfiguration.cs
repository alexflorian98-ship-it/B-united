using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class GuidanceResponseConfiguration : IEntityTypeConfiguration<GuidanceResponse>
{
    public void Configure(EntityTypeBuilder<GuidanceResponse> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Body).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();

        builder.HasIndex(g => g.AuthorUserId);
        builder.HasIndex(g => new { g.QuestionnaireSubmissionId, g.Version }).IsUnique();
        builder.HasOne<QuestionnaireSubmission>().WithMany().HasForeignKey(g => g.QuestionnaireSubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
