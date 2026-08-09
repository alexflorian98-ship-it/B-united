namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed record AddQuizOptionRequest(string Language, string Label, bool IsCorrect);

public sealed record AddQuizOptionCommand(Guid QuizQuestionId, string Language, string Label, bool IsCorrect);
