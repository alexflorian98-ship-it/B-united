namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

public sealed record AnswerFollowUpRequest(string Answer);

public sealed record AnswerFollowUpCommand(Guid FollowUpId, string Answer);
