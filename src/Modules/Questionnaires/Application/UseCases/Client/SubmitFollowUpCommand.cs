namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed record SubmitFollowUpRequest(string Question);

public sealed record SubmitFollowUpCommand(Guid UserId, Guid GuidanceResponseId, string Question);
