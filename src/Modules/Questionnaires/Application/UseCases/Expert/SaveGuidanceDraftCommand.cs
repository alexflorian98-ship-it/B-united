namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

public sealed record SaveGuidanceDraftRequest(string Body);

public sealed record SaveGuidanceDraftCommand(Guid SubmissionId, string Body, Guid AuthorUserId);
