namespace BUnited.Modules.Content.Domain;

/// <summary>V1 supports only these two (docs/PROMPT.md §18–22) — deliberately not a dynamic
/// content-type registry; add new types as explicit enum members + handlers, not a plugin
/// system.</summary>
public enum ContentItemType
{
    Video = 0,
    RichText = 1,
}
