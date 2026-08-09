namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed record ReorderQuizOptionsRequest(IReadOnlyList<Guid> OrderedQuizOptionIds);

public sealed record ReorderQuizOptionsCommand(Guid QuizQuestionId, IReadOnlyList<Guid> OrderedQuizOptionIds);
