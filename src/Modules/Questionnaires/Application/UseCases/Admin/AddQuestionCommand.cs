namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record AddQuestionRequest(string Type, bool IsRequired, string Text, string? HelpText);

public sealed record AddQuestionCommand(Guid QuestionnaireId, string Type, bool IsRequired, string Text, string? HelpText, Guid ActorId);
