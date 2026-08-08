using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

/// <summary>docs/PROMPT.md §25–28: "Do not overwrite published guidance silently — if edited
/// after publication, preserve history via a simple version number." <see cref="PublishedAt"/>
/// is null while the expert is still drafting; once set, <see cref="Body"/> becomes immutable
/// and any further edit must create a new row with <see cref="Version"/> + 1, not mutate this
/// one. Treated as high-sensitivity content (§35), same handling as <see cref="QuestionnaireAnswer"/>.</summary>
public sealed class GuidanceResponse : IAuditableEntity
{
    private GuidanceResponse()
    {
    }

    public static GuidanceResponse CreateDraft(Guid submissionId, Guid authorUserId, int version, string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuestionnaireSubmissionId = submissionId,
            AuthorUserId = authorUserId,
            Version = version,
            Body = body,
        };

    public Guid Id { get; private set; }

    public Guid QuestionnaireSubmissionId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public int Version { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; private set; }

    public void UpdateDraftBody(string body)
    {
        if (PublishedAt is not null)
        {
            throw new InvalidOperationException(
                "Published guidance is immutable — create a new version instead of editing this one.");
        }

        Body = body;
    }

    public void Publish(DateTime utcNow)
    {
        if (PublishedAt is not null)
        {
            throw new InvalidOperationException("This guidance version is already published.");
        }

        PublishedAt = utcNow;
    }
}
