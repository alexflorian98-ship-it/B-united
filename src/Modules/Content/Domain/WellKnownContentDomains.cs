namespace BUnited.Modules.Content.Domain;

/// <summary>The 5 initial content domains (docs/PROMPT.md §18–22), each with a stable seed ID
/// and slug — mirrors the <c>WellKnownRoles</c>/<c>WellKnownPermissions</c> pattern in the
/// Identity module.</summary>
public static class WellKnownContentDomains
{
    public const string Psychology = "psychology";
    public const string Sport = "sport";
    public const string Nutrition = "nutrition";
    public const string Business = "business";
    public const string FinancialEducation = "financial-education";

    public static readonly IReadOnlyList<(Guid Id, string Slug, int SortOrder)> All =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000301"), Psychology, 1),
        (Guid.Parse("00000000-0000-0000-0000-000000000302"), Sport, 2),
        (Guid.Parse("00000000-0000-0000-0000-000000000303"), Nutrition, 3),
        (Guid.Parse("00000000-0000-0000-0000-000000000304"), Business, 4),
        (Guid.Parse("00000000-0000-0000-0000-000000000305"), FinancialEducation, 5),
    ];
}
