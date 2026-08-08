namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record ReorderQuestionsRequest(IReadOnlyList<Guid> OrderedQuestionIds);

public sealed record ReorderQuestionsCommand(Guid QuestionnaireId, IReadOnlyList<Guid> OrderedQuestionIds);
