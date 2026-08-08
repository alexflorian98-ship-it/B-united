using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.Abstractions;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed record VideoPlaybackResult(string PlaybackUrl, string? ThumbnailUrl);

/// <summary>Gates video playback on active <c>PlatformAccess</c> before ever returning a
/// playback URL (docs/PROMPT.md §18–22) — via the temporary <see cref="IAccessContext"/> stub
/// until Billing lands in Phase 3 (P2.09.c/P3.15). See ADR-005 for the real caveat: the
/// URL itself isn't short-lived/signed in V1 (YouTube), so this check is the only actual access
/// control point.</summary>
public sealed class GetVideoPlaybackHandler(DbContext dbContext, IAccessContext accessContext, IVideoProvider videoProvider)
{
    public async Task<VideoPlaybackResult> HandleAsync(Guid contentItemId, Guid userId, CancellationToken cancellationToken)
    {
        var hasAccess = await accessContext.HasActivePlatformAccessAsync(userId, cancellationToken);
        if (!hasAccess)
        {
            throw new BusinessRuleAppException("PLATFORM_ACCESS_REQUIRED", "errors.platformAccessRequired", "An active subscription is required to play this video.");
        }

        var item = await dbContext.Set<ContentItem>().SingleOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new NotFoundAppException("The specified content item does not exist.");

        if (item.Type != ContentItemType.Video || item.MediaAssetId is null)
        {
            throw new BusinessRuleAppException("CONTENT_ITEM_NOT_A_VIDEO", "errors.contentItem.notAVideo", "This content item is not a video.");
        }

        var mediaAsset = await dbContext.Set<MediaAsset>().SingleAsync(m => m.Id == item.MediaAssetId, cancellationToken);
        var playbackInfo = await videoProvider.GetPlaybackInfoAsync(mediaAsset.ProviderAssetId, cancellationToken);

        return new VideoPlaybackResult(playbackInfo.PlaybackUrl, playbackInfo.ThumbnailUrl);
    }
}
