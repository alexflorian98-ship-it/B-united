namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed record UpsertEventTranslationRequest(string Language, string Title, string Description);

public sealed record UpsertEventTranslationCommand(Guid EventId, string Language, string Title, string Description);
