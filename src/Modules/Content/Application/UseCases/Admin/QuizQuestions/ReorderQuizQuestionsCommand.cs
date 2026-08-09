namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed record ReorderQuizQuestionsRequest(IReadOnlyList<Guid> OrderedQuizQuestionIds);

public sealed record ReorderQuizQuestionsCommand(Guid ContentItemId, IReadOnlyList<Guid> OrderedQuizQuestionIds);
