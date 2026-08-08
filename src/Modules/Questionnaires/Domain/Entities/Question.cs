using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

public sealed class Question : IAuditableEntity
{
    private Question()
    {
    }

    public static Question Create(Guid questionnaireId, QuestionType type, int sortOrder, bool isRequired, Guid? createdBy) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionnaireId = questionnaireId,
            Type = type,
            SortOrder = sortOrder,
            IsRequired = isRequired,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

    public Guid Id { get; private set; }

    public Guid QuestionnaireId { get; private set; }

    public QuestionType Type { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsRequired { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; private set; }

    public void Reorder(int sortOrder, Guid updatedBy)
    {
        SortOrder = sortOrder;
        UpdatedBy = updatedBy;
    }
}
