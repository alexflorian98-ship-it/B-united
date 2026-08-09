namespace BUnited.Modules.Content.Domain.Entities;

/// <summary>Append-only record of one quiz submission (one row per submission/retake — never
/// updated). <see cref="ContentItemId"/> is a plain, opaque reference (no FK) to the owning
/// <see cref="ContentItem"/>, matching the same "no FK across an aggregate the entity doesn't
/// need to join through EF" convention used by <c>ContentProgress.ContentItemId</c>. Owned by
/// Content (not Progress) because grading requires the correct-answer data only Content
/// has.</summary>
public sealed class QuizAttempt
{
    private QuizAttempt()
    {
    }

    public static QuizAttempt Create(Guid userId, Guid contentItemId, int totalQuestions, int correctCount, DateTime submittedAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContentItemId = contentItemId,
            TotalQuestions = totalQuestions,
            CorrectCount = correctCount,
            SubmittedAtUtc = submittedAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ContentItemId { get; private set; }

    public int TotalQuestions { get; private set; }

    public int CorrectCount { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }
}
