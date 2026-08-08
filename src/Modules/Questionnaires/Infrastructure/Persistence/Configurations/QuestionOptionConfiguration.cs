using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Value).IsRequired().HasMaxLength(200);

        builder.HasIndex(o => new { o.QuestionId, o.Value }).IsUnique();
        builder.HasOne<Question>().WithMany().HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
