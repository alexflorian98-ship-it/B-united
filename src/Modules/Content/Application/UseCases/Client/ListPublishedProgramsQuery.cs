namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed record ListPublishedProgramsQuery(Guid? DomainId, string RequestedLanguage);
