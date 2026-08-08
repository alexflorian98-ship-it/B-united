namespace BUnited.Modules.Progress.Application.UseCases;

public sealed record RecordVideoProgressRequest(
    Guid ContentItemId,
    Guid SectionId,
    IReadOnlyList<Guid> SectionContentItemIds,
    int PositionSeconds,
    double WatchPercentage);

public sealed record RecordVideoProgressCommand(
    Guid UserId,
    Guid ContentItemId,
    Guid SectionId,
    IReadOnlyList<Guid> SectionContentItemIds,
    int PositionSeconds,
    double WatchPercentage);
