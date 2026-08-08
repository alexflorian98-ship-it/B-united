namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed record ReorderContentItemsRequest(IReadOnlyList<Guid> OrderedContentItemIds);

public sealed record ReorderContentItemsCommand(Guid SectionId, IReadOnlyList<Guid> OrderedContentItemIds);
