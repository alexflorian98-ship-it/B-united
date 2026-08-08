namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed record AnswerInput(Guid QuestionId, string Value);

public sealed record SaveDraftAnswersRequest(IReadOnlyList<AnswerInput> Answers);

public sealed record SaveDraftAnswersCommand(Guid UserId, Guid SubmissionId, IReadOnlyList<AnswerInput> Answers);
