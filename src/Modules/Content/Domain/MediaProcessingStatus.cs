namespace BUnited.Modules.Content.Domain;

/// <summary>docs/PROMPT.md §18–22.</summary>
public enum MediaProcessingStatus
{
    Uploading = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3,
}
