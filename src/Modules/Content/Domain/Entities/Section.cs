using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class Section : IAuditableEntity
{
    private Section()
    {
    }

    public static Section Create(Guid programId, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProgramId = programId,
            SortOrder = sortOrder,
            Status = ContentStatus.Draft,
        };

    public Guid Id { get; private set; }

    public Guid ProgramId { get; private set; }

    public int SortOrder { get; private set; }

    public ContentStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Reorder(int sortOrder) => SortOrder = sortOrder;

    public void Publish() => Status = ContentStatus.Published;

    public void Unpublish() => Status = ContentStatus.Draft;

    public void Archive() => Status = ContentStatus.Archived;
}
