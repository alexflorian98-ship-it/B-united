using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class QuizQuestionTranslation : ITranslation
{
    private QuizQuestionTranslation()
    {
    }

    public static QuizQuestionTranslation Create(Guid quizQuestionId, string language, string text) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuizQuestionId = quizQuestionId,
            Language = language,
            Text = text,
        };

    public Guid Id { get; private set; }

    public Guid QuizQuestionId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;

    public void Update(string text)
    {
        Text = text;
    }
}
