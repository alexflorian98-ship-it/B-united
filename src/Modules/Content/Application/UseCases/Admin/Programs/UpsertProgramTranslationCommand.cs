namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed record UpsertProgramTranslationRequest(string Language, string Title, string ShortDescription, string Description);

public sealed record UpsertProgramTranslationCommand(Guid ProgramId, string Language, string Title, string ShortDescription, string Description);
