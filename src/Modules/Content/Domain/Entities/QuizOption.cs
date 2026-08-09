namespace BUnited.Modules.Content.Domain.Entities;

/// <summary>A selectable answer choice for a <see cref="QuizQuestion"/>. <see cref="IsCorrect"/>
/// is the first "correct answer" concept in this codebase and is exclusively a server-side
/// grading fact — it MUST NEVER be serialized into any client-facing DTO before that question has
/// been submitted (docs/DEVELOPMENT_INSTRUCTIONS.md §6). V1 is single-correct-answer/single-select
/// (see Phase 1 plan), so at most one <see cref="QuizOption"/> per <see cref="QuizQuestion"/> may
/// have <see cref="IsCorrect"/> <see langword="true"/> — enforced at add-time by
/// <c>AddQuizOptionHandler</c>.</summary>
public sealed class QuizOption
{
    private QuizOption()
    {
    }

    public static QuizOption Create(Guid quizQuestionId, bool isCorrect, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuizQuestionId = quizQuestionId,
            IsCorrect = isCorrect,
            SortOrder = sortOrder,
        };

    public Guid Id { get; private set; }

    public Guid QuizQuestionId { get; private set; }

    public bool IsCorrect { get; private set; }

    public int SortOrder { get; private set; }

    public void Reorder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
