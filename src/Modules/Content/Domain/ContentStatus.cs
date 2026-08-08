namespace BUnited.Modules.Content.Domain;

/// <summary>Shared by <see cref="Entities.Program"/> and <see cref="Entities.Section"/>
/// (docs/PROMPT.md §18–22).</summary>
public enum ContentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}
