using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class QuizOptionTranslation : ITranslation
{
    private QuizOptionTranslation()
    {
    }

    public static QuizOptionTranslation Create(Guid quizOptionId, string language, string label) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuizOptionId = quizOptionId,
            Language = language,
            Label = label,
        };

    public Guid Id { get; private set; }

    public Guid QuizOptionId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public void Update(string label)
    {
        Label = label;
    }
}
