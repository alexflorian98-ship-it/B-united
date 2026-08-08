using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Content.Domain.Entities;

/// <summary>docs/PROMPT.md §18–22. Optimistic concurrency uses Postgres's native <c>xmin</c>
/// system column (configured in <c>ProgramConfiguration</c>), not a CLR-visible token
/// property. <see cref="CreatedAt"/>/<see cref="UpdatedAt"/> are stamped automatically by
/// <c>AuditableEntitySaveChangesInterceptor</c>.</summary>
public sealed class Program : IAuditableEntity
{
    private Program()
    {
    }

    public static Program Create(Guid domainId, string slug, string defaultLanguage, Guid? createdBy) =>
        new()
        {
            Id = Guid.NewGuid(),
            DomainId = domainId,
            Slug = slug,
            Status = ContentStatus.Draft,
            DefaultLanguage = defaultLanguage,
            SortOrder = 0,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

    public Guid Id { get; private set; }

    public Guid DomainId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public ContentStatus Status { get; private set; }

    public string DefaultLanguage { get; private set; } = string.Empty;

    public Guid? CoverAssetId { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; private set; }

    public void SetCoverAsset(Guid? mediaAssetId, Guid updatedBy)
    {
        CoverAssetId = mediaAssetId;
        UpdatedBy = updatedBy;
    }

    public void Reorder(int sortOrder, Guid updatedBy)
    {
        SortOrder = sortOrder;
        UpdatedBy = updatedBy;
    }

    public void Publish(Guid updatedBy)
    {
        if (Status == ContentStatus.Archived)
        {
            throw new InvalidOperationException("An archived program cannot be published again.");
        }

        Status = ContentStatus.Published;
        UpdatedBy = updatedBy;
    }

    public void Unpublish(Guid updatedBy)
    {
        if (Status != ContentStatus.Published)
        {
            throw new InvalidOperationException("Only a published program can be unpublished.");
        }

        Status = ContentStatus.Draft;
        UpdatedBy = updatedBy;
    }

    public void Archive(Guid updatedBy)
    {
        if (Status == ContentStatus.Archived)
        {
            throw new InvalidOperationException("The program is already archived.");
        }

        Status = ContentStatus.Archived;
        UpdatedBy = updatedBy;
    }
}
