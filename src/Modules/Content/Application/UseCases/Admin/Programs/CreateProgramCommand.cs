namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed record CreateProgramRequest(Guid DomainId, string Slug, string DefaultLanguage, string Title, string ShortDescription, string Description);

public sealed record CreateProgramCommand(
    Guid DomainId,
    string Slug,
    string DefaultLanguage,
    string Title,
    string ShortDescription,
    string Description,
    Guid ActorId);
