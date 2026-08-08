using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class ContentItem : IAuditableEntity
{
    private ContentItem()
    {
    }

    public static ContentItem Create(Guid sectionId, ContentItemType type, int sortOrder, bool isRequired, Guid? mediaAssetId)
    {
        if (type == ContentItemType.Video && mediaAssetId is null)
        {
            throw new ArgumentException("A video content item requires a media asset.", nameof(mediaAssetId));
        }

        return new ContentItem
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Type = type,
            SortOrder = sortOrder,
            IsRequired = isRequired,
            MediaAssetId = mediaAssetId,
        };
    }

    public Guid Id { get; private set; }

    public Guid SectionId { get; private set; }

    public ContentItemType Type { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsRequired { get; private set; }

    public Guid? MediaAssetId { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Reorder(int sortOrder) => SortOrder = sortOrder;

    public void SetRequired(bool isRequired) => IsRequired = isRequired;

    public void SetMediaAsset(Guid mediaAssetId)
    {
        if (Type != ContentItemType.Video)
        {
            throw new InvalidOperationException("Only a video content item can have a media asset.");
        }

        MediaAssetId = mediaAssetId;
    }
}
