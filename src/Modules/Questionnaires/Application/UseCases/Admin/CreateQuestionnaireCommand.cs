namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record CreateQuestionnaireRequest(string DefaultLanguage, string Title, string Description);

public sealed record CreateQuestionnaireCommand(string DefaultLanguage, string Title, string Description, Guid ActorId);
