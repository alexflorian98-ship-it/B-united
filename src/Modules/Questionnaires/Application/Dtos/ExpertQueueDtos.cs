namespace BUnited.Modules.Questionnaires.Application.Dtos;

/// <summary>§25–28: server-computed waiting-time bucket, for consistent UI rendering across
/// clients (P4.10.b) — never computed client-side.</summary>
public enum WaitingTimeBucket
{
    Under24Hours,
    Between24And48Hours,
    Over48Hours,
}

public sealed record QueueItemDto(
    Guid SubmissionId,
    Guid UserId,
    string? ClientEmail,
    DateTime SubmittedAt,
    WaitingTimeBucket WaitingBucket,
    string Status);

public sealed record QAPairDto(
    Guid QuestionId,
    string QuestionText,
    string Type,
    string AnswerValue,
    IReadOnlyList<string>? AnswerLabels);

public sealed record GuidanceVersionDto(
    Guid Id,
    int Version,
    string Body,
    DateTime? PublishedAt,
    GuidanceFollowUpDto? FollowUp);

public sealed record SubmissionDetailForExpertDto(
    Guid SubmissionId,
    Guid UserId,
    string? ClientEmail,
    DateTime? SubmittedAt,
    IReadOnlyList<QAPairDto> Answers,
    IReadOnlyList<GuidanceVersionDto> GuidanceHistory);
