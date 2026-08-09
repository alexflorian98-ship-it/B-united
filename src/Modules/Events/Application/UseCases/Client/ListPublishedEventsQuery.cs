namespace BUnited.Modules.Events.Application.UseCases.Client;

public sealed record ListPublishedEventsQuery(Guid UserId, string Language, bool IncludePast);
