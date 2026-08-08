namespace BUnited.Modules.Content.Application.Abstractions;

public sealed record VideoRegistration(string ProviderAssetId, string? ThumbnailUrl, int? DurationSeconds);

public sealed record VideoPlaybackInfo(string PlaybackUrl, string? ThumbnailUrl);

/// <summary>
/// Decouples video-content authoring/playback from the specific hosting provider (see
/// ADR-005). The only implementation for V1 is <c>YouTubeVideoProvider</c> — swapping to a
/// provider with real signed/short-lived URLs later means adding a new adapter, not changing
/// any caller of this interface.
/// </summary>
public interface IVideoProvider
{
    string ProviderName { get; }

    /// <summary>Validates and normalizes an expert-supplied video reference (URL or raw ID)
    /// into a stable provider asset ID. Throws <see cref="ArgumentException"/> if it isn't a
    /// recognizable reference for this provider.</summary>
    Task<VideoRegistration> RegisterExistingAsync(string externalReference, CancellationToken cancellationToken);

    Task<VideoPlaybackInfo> GetPlaybackInfoAsync(string providerAssetId, CancellationToken cancellationToken);
}
