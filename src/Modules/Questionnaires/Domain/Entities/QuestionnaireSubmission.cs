using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

/// <summary>docs/PROMPT.md §25–28. Operational timestamps are retained even with a single
/// expert, to enable metrics (average response time, oldest unanswered submission, waiting-time
/// buckets, monthly expert workload) — never overwritten once set (each "Mark*" method is a
/// no-op if the timestamp already exists).</summary>
public sealed class QuestionnaireSubmission : IAuditableEntity
{
    private QuestionnaireSubmission()
    {
    }

    public static QuestionnaireSubmission Start(Guid userId, Guid questionnaireId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionnaireId = questionnaireId,
            Status = QuestionnaireSubmissionStatus.Draft,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid QuestionnaireId { get; private set; }

    public QuestionnaireSubmissionStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? SubmittedAt { get; private set; }

    public DateTime? AssignedAt { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public DateTime? AnsweredAt { get; private set; }

    public void MarkStarted(DateTime utcNow)
    {
        StartedAt ??= utcNow;
    }

    public void Submit(DateTime utcNow)
    {
        if (Status != QuestionnaireSubmissionStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft submission can be submitted.");
        }

        Status = QuestionnaireSubmissionStatus.Submitted;
        SubmittedAt = utcNow;
    }

    public void MarkAssigned(DateTime utcNow)
    {
        AssignedAt ??= utcNow;
    }

    public void MarkReviewed(DateTime utcNow)
    {
        ReviewedAt ??= utcNow;
    }

    public void MarkAnswered(DateTime utcNow)
    {
        if (Status != QuestionnaireSubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted submission can receive published guidance.");
        }

        Status = QuestionnaireSubmissionStatus.Answered;
        AnsweredAt = utcNow;
    }
}
