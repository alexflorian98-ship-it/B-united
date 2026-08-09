namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed record AddQuizQuestionRequest(string Language, string Text);

public sealed record AddQuizQuestionCommand(Guid ContentItemId, string Language, string Text);
