using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

/// <summary>One answer to one question within a submission. <see cref="Value"/> holds the raw
/// answer: free text for Text/LongText, the selected <see cref="QuestionOption.Value"/> for
/// SingleChoice/Scale, a comma-separated list of option values for MultiChoice. Treated as
/// high-sensitivity content end to end (§35) — never logged, never in ordinary analytics.
/// Encryption at rest is explicitly deferred per ADR-009; access control (P4.15) and audit
/// (P4.17) are the enforced protections for V1.</summary>
public sealed class QuestionnaireAnswer : IAuditableEntity
{
    private QuestionnaireAnswer()
    {
    }

    public static QuestionnaireAnswer Create(Guid submissionId, Guid questionId, string value) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionnaireSubmissionId = submissionId,
            QuestionId = questionId,
            Value = value,
        };

    public Guid Id { get; private set; }

    public Guid QuestionnaireSubmissionId { get; private set; }

    public Guid QuestionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void UpdateValue(string value)
    {
        Value = value;
    }
}
