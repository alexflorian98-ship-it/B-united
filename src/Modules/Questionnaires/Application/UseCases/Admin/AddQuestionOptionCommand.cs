namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record AddQuestionOptionRequest(string Value, string Label);

public sealed record AddQuestionOptionCommand(Guid QuestionId, string Value, string Label);
