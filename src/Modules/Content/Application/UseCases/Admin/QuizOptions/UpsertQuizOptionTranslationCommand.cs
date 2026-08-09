namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed record UpsertQuizOptionTranslationRequest(string Language, string Label);

public sealed record UpsertQuizOptionTranslationCommand(Guid QuizOptionId, string Language, string Label);
