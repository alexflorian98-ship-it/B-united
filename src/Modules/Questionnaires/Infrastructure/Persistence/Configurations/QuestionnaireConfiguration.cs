using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class QuestionnaireConfiguration : IEntityTypeConfiguration<Questionnaire>
{
    public void Configure(EntityTypeBuilder<Questionnaire> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.ProgramId).IsRequired();
        builder.Property(q => q.DefaultLanguage).IsRequired().HasMaxLength(10);
        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(q => q.CreatedAt).IsRequired();
        builder.Property(q => q.UpdatedAt).IsRequired();
    }
}
