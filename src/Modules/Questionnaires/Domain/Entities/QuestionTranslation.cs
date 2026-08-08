using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

public sealed class QuestionTranslation : ITranslation
{
    private QuestionTranslation()
    {
    }

    public static QuestionTranslation Create(Guid questionId, string language, string text, string? helpText) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Language = language,
            Text = text,
            HelpText = helpText,
        };

    public Guid Id { get; private set; }

    public Guid QuestionId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;

    public string? HelpText { get; private set; }

    public void Update(string text, string? helpText)
    {
        Text = text;
        HelpText = helpText;
    }
}
