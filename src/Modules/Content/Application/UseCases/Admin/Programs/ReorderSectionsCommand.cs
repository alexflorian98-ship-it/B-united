namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed record ReorderSectionsRequest(IReadOnlyList<Guid> OrderedSectionIds);

public sealed record ReorderSectionsCommand(Guid ProgramId, IReadOnlyList<Guid> OrderedSectionIds);
