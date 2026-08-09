using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
{
    public void Configure(EntityTypeBuilder<QuizOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.IsCorrect).IsRequired();

        builder.HasIndex(o => o.QuizQuestionId);
        builder.HasOne<QuizQuestion>().WithMany().HasForeignKey(o => o.QuizQuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
