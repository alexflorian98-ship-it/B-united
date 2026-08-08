namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record UpsertQuestionTranslationRequest(string Language, string Text, string? HelpText);

public sealed record UpsertQuestionTranslationCommand(Guid QuestionId, string Language, string Text, string? HelpText);
