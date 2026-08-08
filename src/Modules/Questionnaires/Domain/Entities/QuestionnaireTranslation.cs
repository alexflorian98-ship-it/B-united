using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

public sealed class QuestionnaireTranslation : ITranslation
{
    private QuestionnaireTranslation()
    {
    }

    public static QuestionnaireTranslation Create(Guid questionnaireId, string language, string title, string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionnaireId = questionnaireId,
            Language = language,
            Title = title,
            Description = description,
        };

    public Guid Id { get; private set; }

    public Guid QuestionnaireId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
    }
}
