namespace BUnited.Modules.Questionnaires.Application.Dtos;

public sealed record ClientQuestionOptionDto(string Value, string Label);

public sealed record ClientQuestionDto(
    Guid Id,
    string Type,
    bool IsRequired,
    int SortOrder,
    string Text,
    string? HelpText,
    IReadOnlyList<ClientQuestionOptionDto> Options);

public sealed record ClientQuestionnaireDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<ClientQuestionDto> Questions);

public sealed record SubmissionAnswerDto(Guid QuestionId, string Value);

public sealed record MySubmissionDto(
    Guid SubmissionId,
    Guid QuestionnaireId,
    string Status,
    DateTime? StartedAt,
    DateTime? SubmittedAt,
    IReadOnlyList<SubmissionAnswerDto> Answers);

public sealed record GuidanceFollowUpDto(Guid Id, string Question, string? Answer, DateTime? AnsweredAt);

public sealed record GuidanceDto(
    Guid Id,
    Guid SubmissionId,
    int Version,
    string Body,
    DateTime PublishedAt,
    GuidanceFollowUpDto? FollowUp);
