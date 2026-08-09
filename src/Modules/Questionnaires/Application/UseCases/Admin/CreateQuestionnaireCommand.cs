namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record CreateQuestionnaireRequest(Guid ProgramId, string DefaultLanguage, string Title, string Description);

public sealed record CreateQuestionnaireCommand(Guid ProgramId, string DefaultLanguage, string Title, string Description, Guid ActorId);
