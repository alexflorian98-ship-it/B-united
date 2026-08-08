namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed record UpsertContentItemTranslationRequest(string Language, string Title, string? Body);

public sealed record UpsertContentItemTranslationCommand(Guid ContentItemId, string Language, string Title, string? Body);
