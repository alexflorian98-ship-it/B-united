namespace BUnited.Modules.Progress.Application.UseCases;

public sealed record MarkContentCompletedRequest(Guid ContentItemId, Guid SectionId, IReadOnlyList<Guid> SectionContentItemIds);

public sealed record MarkContentCompletedCommand(Guid UserId, Guid ContentItemId, Guid SectionId, IReadOnlyList<Guid> SectionContentItemIds);
