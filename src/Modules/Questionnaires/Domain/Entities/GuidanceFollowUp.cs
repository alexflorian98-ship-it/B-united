using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Questionnaires.Domain.Entities;

/// <summary>docs/PROMPT.md §25–28: "One bounded follow-up question is allowed after a guidance
/// response — this is NOT direct messaging." The one-per-guidance limit is enforced by a unique
/// index on <see cref="GuidanceResponseId"/> at the database layer (QuestionnairesInfrastructure
/// configuration), not just an application-layer check, per the "unique business invariants
/// MUST be protected by database constraints" rule.</summary>
public sealed class GuidanceFollowUp : IAuditableEntity
{
    private GuidanceFollowUp()
    {
    }

    public static GuidanceFollowUp Ask(Guid guidanceResponseId, string question) =>
        new()
        {
            Id = Guid.NewGuid(),
            GuidanceResponseId = guidanceResponseId,
            Question = question,
        };

    public Guid Id { get; private set; }

    public Guid GuidanceResponseId { get; private set; }

    public string Question { get; private set; } = string.Empty;

    public string? Answer { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? AnsweredAt { get; private set; }

    public void Respond(string answer, DateTime utcNow)
    {
        if (AnsweredAt is not null)
        {
            throw new InvalidOperationException("This follow-up has already been answered.");
        }

        Answer = answer;
        AnsweredAt = utcNow;
    }
}
