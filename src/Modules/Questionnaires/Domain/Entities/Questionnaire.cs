using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

/// <summary>docs/PROMPT.md §25–28.</summary>
public sealed class Questionnaire : IAuditableEntity
{
    private Questionnaire()
    {
    }

    public static Questionnaire Create(string defaultLanguage, Guid? createdBy) =>
        new()
        {
            Id = Guid.NewGuid(),
            DefaultLanguage = defaultLanguage,
            Status = QuestionnaireStatus.Draft,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

    public Guid Id { get; private set; }

    public string DefaultLanguage { get; private set; } = string.Empty;

    public QuestionnaireStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; private set; }

    public void Publish(Guid updatedBy)
    {
        if (Status == QuestionnaireStatus.Archived)
        {
            throw new InvalidOperationException("An archived questionnaire cannot be published again.");
        }

        Status = QuestionnaireStatus.Published;
        UpdatedBy = updatedBy;
    }

    public void Unpublish(Guid updatedBy)
    {
        if (Status != QuestionnaireStatus.Published)
        {
            throw new InvalidOperationException("Only a published questionnaire can be unpublished.");
        }

        Status = QuestionnaireStatus.Draft;
        UpdatedBy = updatedBy;
    }

    public void Archive(Guid updatedBy)
    {
        if (Status == QuestionnaireStatus.Archived)
        {
            throw new InvalidOperationException("The questionnaire is already archived.");
        }

        Status = QuestionnaireStatus.Archived;
        UpdatedBy = updatedBy;
    }
}
