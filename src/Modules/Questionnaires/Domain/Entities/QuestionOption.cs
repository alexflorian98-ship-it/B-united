namespace BUnited.Modules.Questionnaires.Domain.Entities;

/// <summary>A selectable choice for a <see cref="QuestionType.SingleChoice"/>/<see cref="QuestionType.MultiChoice"/>
/// question, or a scale point for <see cref="QuestionType.Scale"/> (e.g. "1".."5"). Not used for
/// Text/LongText questions.</summary>
public sealed class QuestionOption
{
    private QuestionOption()
    {
    }

    public static QuestionOption Create(Guid questionId, string value, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Value = value,
            SortOrder = sortOrder,
        };

    public Guid Id { get; private set; }

    public Guid QuestionId { get; private set; }

    /// <summary>Stable machine value used in <see cref="QuestionnaireAnswer.Value"/> — not shown to
    /// users directly; the visible label lives in <see cref="QuestionOptionTranslation"/>.</summary>
    public string Value { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public void Reorder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
