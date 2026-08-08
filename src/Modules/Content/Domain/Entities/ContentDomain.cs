namespace BUnited.Modules.Content.Domain.Entities;

/// <summary>
/// A top-level content category (Psychology, Sport, Nutrition, Business, FinancialEducation —
/// docs/PROMPT.md §18–22). Named <c>ContentDomain</c> rather than the spec's bare "Domain" to
/// avoid colliding with this project's own <c>BUnited.Modules.Content.Domain</c> namespace
/// segment (docs/HANDOVER.md: never name a type identically to a namespace segment it lives
/// under). Display name is a fixed, small set resolved client-side via i18n
/// (<c>content.json</c>), not a DB translation table — unlike Program/Section/ContentItem,
/// which have genuinely free-form expert-authored text.
/// </summary>
public sealed class ContentDomain
{
    private ContentDomain()
    {
    }

    public static ContentDomain Create(Guid id, string slug, int sortOrder) =>
        new()
        {
            Id = id,
            Slug = slug,
            SortOrder = sortOrder,
        };

    public Guid Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }
}
