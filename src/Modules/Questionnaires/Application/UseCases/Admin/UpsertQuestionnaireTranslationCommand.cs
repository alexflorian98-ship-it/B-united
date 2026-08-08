namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record UpsertQuestionnaireTranslationRequest(string Language, string Title, string Description);

public sealed record UpsertQuestionnaireTranslationCommand(Guid QuestionnaireId, string Language, string Title, string Description);
