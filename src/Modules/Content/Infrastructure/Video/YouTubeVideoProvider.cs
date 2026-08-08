using System.Text.RegularExpressions;
using BUnited.Modules.Content.Application.Abstractions;

namespace BUnited.Modules.Content.Infrastructure.Video;

/// <summary>V1's only <see cref="IVideoProvider"/> adapter (see ADR-005) — the expert pastes an
/// existing YouTube video URL/ID; there is no upload/transcode step, so registration is
/// synchronous under the hood despite the async interface (kept async for when a real
/// upload-based provider replaces this).</summary>
public sealed class YouTubeVideoProvider : IVideoProvider
{
    private static readonly Regex VideoIdPattern = new("^[a-zA-Z0-9_-]{11}$", RegexOptions.Compiled);

    private static readonly Regex[] UrlPatterns =
    [
        new(@"(?:youtube\.com/watch\?v=|youtube\.com/embed/|youtu\.be/|youtube\.com/shorts/)(?<id>[a-zA-Z0-9_-]{11})", RegexOptions.Compiled),
    ];

    public string ProviderName => "youtube";

    public Task<VideoRegistration> RegisterExistingAsync(string externalReference, CancellationToken cancellationToken)
    {
        var videoId = ExtractVideoId(externalReference);
        return Task.FromResult(new VideoRegistration(videoId, BuildThumbnailUrl(videoId), DurationSeconds: null));
    }

    public Task<VideoPlaybackInfo> GetPlaybackInfoAsync(string providerAssetId, CancellationToken cancellationToken) =>
        Task.FromResult(new VideoPlaybackInfo($"https://www.youtube.com/embed/{providerAssetId}", BuildThumbnailUrl(providerAssetId)));

    private static string BuildThumbnailUrl(string videoId) => $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

    internal static string ExtractVideoId(string externalReference)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
        {
            throw new ArgumentException("A YouTube video URL or ID is required.", nameof(externalReference));
        }

        var trimmed = externalReference.Trim();

        if (VideoIdPattern.IsMatch(trimmed))
        {
            return trimmed;
        }

        foreach (var pattern in UrlPatterns)
        {
            var match = pattern.Match(trimmed);
            if (match.Success)
            {
                return match.Groups["id"].Value;
            }
        }

        throw new ArgumentException($"'{externalReference}' is not a recognizable YouTube video URL or ID.", nameof(externalReference));
    }
}
