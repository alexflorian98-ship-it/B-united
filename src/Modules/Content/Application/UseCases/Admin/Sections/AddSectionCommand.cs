namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed record AddSectionRequest(string Language, string Title, string Description);

public sealed record AddSectionCommand(Guid ProgramId, string Language, string Title, string Description);
