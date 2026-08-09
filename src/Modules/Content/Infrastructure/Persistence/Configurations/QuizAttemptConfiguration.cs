using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

/// <summary>Append-only history table — <see cref="QuizAttempt.ContentItemId"/> is a plain,
/// opaque <see cref="Guid"/> (no FK), matching <c>ContentProgress.ContentItemId</c>'s existing
/// convention; it references the same <see cref="ContentItem"/> aggregate this configuration's
/// assembly already owns, so no cross-module boundary is crossed, but a real FK isn't needed for
/// any query this entity supports.</summary>
public sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TotalQuestions).IsRequired();
        builder.Property(a => a.CorrectCount).IsRequired();
        builder.Property(a => a.SubmittedAtUtc).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.ContentItemId });
    }
}
