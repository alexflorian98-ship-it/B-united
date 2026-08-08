using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;

namespace BUnited.Modules.Content.Tests.Domain;

public sealed class ContentItemTests
{
    [Fact]
    public void A_video_item_requires_a_media_asset()
    {
        Assert.Throws<ArgumentException>(() => ContentItem.Create(Guid.NewGuid(), ContentItemType.Video, 0, true, null));
    }

    [Fact]
    public void A_video_item_with_a_media_asset_is_created_successfully()
    {
        var item = ContentItem.Create(Guid.NewGuid(), ContentItemType.Video, 0, true, Guid.NewGuid());

        Assert.Equal(ContentItemType.Video, item.Type);
        Assert.NotNull(item.MediaAssetId);
    }

    [Fact]
    public void A_rich_text_item_does_not_require_a_media_asset()
    {
        var item = ContentItem.Create(Guid.NewGuid(), ContentItemType.RichText, 0, true, null);

        Assert.Equal(ContentItemType.RichText, item.Type);
        Assert.Null(item.MediaAssetId);
    }

    [Fact]
    public void A_media_asset_cannot_be_attached_to_a_rich_text_item()
    {
        var item = ContentItem.Create(Guid.NewGuid(), ContentItemType.RichText, 0, true, null);

        Assert.Throws<InvalidOperationException>(() => item.SetMediaAsset(Guid.NewGuid()));
    }
}
