namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed record UpsertSectionTranslationRequest(string Language, string Title, string Description);

public sealed record UpsertSectionTranslationCommand(Guid SectionId, string Language, string Title, string Description);
