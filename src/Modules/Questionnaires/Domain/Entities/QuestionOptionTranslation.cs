using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

public sealed class QuestionOptionTranslation : ITranslation
{
    private QuestionOptionTranslation()
    {
    }

    public static QuestionOptionTranslation Create(Guid questionOptionId, string language, string label) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionOptionId = questionOptionId,
            Language = language,
            Label = label,
        };

    public Guid Id { get; private set; }

    public Guid QuestionOptionId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public void Update(string label)
    {
        Label = label;
    }
}
