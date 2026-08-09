namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed record UpsertQuizQuestionTranslationRequest(string Language, string Text);

public sealed record UpsertQuizQuestionTranslationCommand(Guid QuizQuestionId, string Language, string Text);
