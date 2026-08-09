namespace BUnited.Modules.Content.Application.UseCases.Client;

/// <summary><paramref name="UserId"/> is <see langword="null"/> only for a caller with no
/// resolvable identity — ownership is then left unresolved on every returned item (P3.37).</summary>
public sealed record ListPublishedProgramsQuery(Guid? DomainId, string RequestedLanguage, Guid? UserId);
