using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Content.Domain.Entities;

/// <summary>A single-select, auto-scored quiz question belonging to a <see cref="ContentItem"/>
/// of <see cref="ContentItemType.Quiz"/>. Mirrors Questionnaires' <c>Question</c> shape; unlike
/// Questionnaires (open-ended, expert-reviewed), grading here is fully automatic via
/// <see cref="QuizOption.IsCorrect"/>.</summary>
public sealed class QuizQuestion : IAuditableEntity
{
    private QuizQuestion()
    {
    }

    public static QuizQuestion Create(Guid contentItemId, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            SortOrder = sortOrder,
        };

    public Guid Id { get; private set; }

    public Guid ContentItemId { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Reorder(int sortOrder) => SortOrder = sortOrder;
}
