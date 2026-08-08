using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class MediaAsset : IAuditableEntity
{
    private MediaAsset()
    {
    }

    public static MediaAsset Create(string provider, string providerAssetId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ProviderAssetId = providerAssetId,
            ProcessingStatus = MediaProcessingStatus.Uploading,
        };

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ProviderAssetId { get; private set; } = string.Empty;

    public string? ProviderPlaybackId { get; private set; }

    public int? DurationSeconds { get; private set; }

    public string? ThumbnailUrl { get; private set; }

    public MediaProcessingStatus ProcessingStatus { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void MarkProcessing() => ProcessingStatus = MediaProcessingStatus.Processing;

    public void MarkReady(string providerPlaybackId, int? durationSeconds, string? thumbnailUrl)
    {
        ProviderPlaybackId = providerPlaybackId;
        DurationSeconds = durationSeconds;
        ThumbnailUrl = thumbnailUrl;
        ProcessingStatus = MediaProcessingStatus.Ready;
    }

    public void MarkFailed() => ProcessingStatus = MediaProcessingStatus.Failed;
}
