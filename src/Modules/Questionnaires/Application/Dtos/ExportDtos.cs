namespace BUnited.Modules.Questionnaires.Application.Dtos;

/// <summary>docs/PROMPT.md §35/§66 — self-service export (P4.19). Deliberately includes only
/// the requesting user's own data; the handler enforces this by construction (always filters
/// on the caller's UserId), never by trusting a caller-supplied id.</summary>
public sealed record ExportedAnswerDto(Guid QuestionId, string Value);

public sealed record ExportedGuidanceDto(int Version, string Body, DateTime? PublishedAt);

public sealed record ExportedSubmissionDto(
    Guid SubmissionId,
    Guid QuestionnaireId,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? SubmittedAt,
    IReadOnlyList<ExportedAnswerDto> Answers,
    IReadOnlyList<ExportedGuidanceDto> Guidance);

public sealed record QuestionnaireDataExportDto(Guid UserId, DateTime ExportedAtUtc, IReadOnlyList<ExportedSubmissionDto> Submissions);
