using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.Abstractions;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed record VideoPlaybackResult(string PlaybackUrl, string? ThumbnailUrl);

/// <summary>Gates video playback on owning the specific program the video belongs to (ADR-003:
/// per-program purchases, not a global subscription) — resolves
/// <c>ContentItem -> Section -> Program</c> server-side, then defers to
/// <see cref="IProgramAccessContext"/>, never trusting a client-supplied program id. See ADR-005
/// for the real caveat: the URL itself isn't short-lived/signed in V1 (YouTube), so this check is
/// the only actual access control point.</summary>
public sealed class GetVideoPlaybackHandler(DbContext dbContext, IProgramAccessContext programAccessContext, IVideoProvider videoProvider)
{
    public async Task<VideoPlaybackResult> HandleAsync(Guid contentItemId, Guid userId, CancellationToken cancellationToken)
    {
        var item = await dbContext.Set<ContentItem>().SingleOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new NotFoundAppException("The specified content item does not exist.");

        var section = await dbContext.Set<Section>().SingleAsync(s => s.Id == item.SectionId, cancellationToken);
        var program = await dbContext.Set<Program>().SingleAsync(p => p.Id == section.ProgramId, cancellationToken);

        await programAccessContext.RequireProgramAccessAsync(userId, program.Id, cancellationToken);

        if (item.Type != ContentItemType.Video || item.MediaAssetId is null)
        {
            throw new BusinessRuleAppException("CONTENT_ITEM_NOT_A_VIDEO", "errors.contentItem.notAVideo", "This content item is not a video.");
        }

        var mediaAsset = await dbContext.Set<MediaAsset>().SingleAsync(m => m.Id == item.MediaAssetId, cancellationToken);
        var playbackInfo = await videoProvider.GetPlaybackInfoAsync(mediaAsset.ProviderAssetId, cancellationToken);

        return new VideoPlaybackResult(playbackInfo.PlaybackUrl, playbackInfo.ThumbnailUrl);
    }
}
