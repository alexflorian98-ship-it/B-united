namespace BUnited.Modules.Questionnaires.Domain;

/// <summary>docs/PROMPT.md §25–28 flow: Draft (save/resume) → Submitted (expert queue) →
/// Answered (guidance published). "Under review" (§41) is a UI-derived state — Submitted with
/// no published guidance yet — not a separate persisted status, since a single-expert V1 has no
/// distinct workflow step between "in the queue" and "being looked at."</summary>
public enum QuestionnaireSubmissionStatus
{
    Draft,
    Submitted,
    Answered,
}
