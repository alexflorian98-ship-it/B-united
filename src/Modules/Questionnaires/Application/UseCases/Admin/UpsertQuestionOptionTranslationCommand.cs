namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record UpsertQuestionOptionTranslationRequest(string Language, string Label);

public sealed record UpsertQuestionOptionTranslationCommand(Guid QuestionOptionId, string Language, string Label);
