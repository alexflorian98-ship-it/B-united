namespace BUnited.Modules.Content.Domain;

/// <summary>V1 supports only these (docs/PROMPT.md §18–22) — deliberately not a dynamic
/// content-type registry; add new types as explicit enum members + handlers, not a plugin
/// system. <see cref="Quiz"/> is a later, explicitly scoped addition (single-correct-answer,
/// single-select, auto-scored) authored/graded entirely within Content — see
/// <c>QuizQuestion</c>/<c>QuizOption</c>/<c>QuizAttempt</c>.</summary>
public enum ContentItemType
{
    Video = 0,
    RichText = 1,
    Quiz = 2,
}
