namespace BUnited.Modules.Identity.Domain;

/// <summary>
/// Stable role identifiers seeded by P1.16. IDs are fixed literals (not <c>Guid.NewGuid()</c>)
/// so seeding is idempotent across environments and re-runs.
/// </summary>
public static class WellKnownRoles
{
    public const string Client = "Client";
    public const string Expert = "Expert";
    public const string Administrator = "Administrator";

    public static readonly Guid ClientId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    public static readonly Guid ExpertId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid AdministratorId = Guid.Parse("00000000-0000-0000-0000-000000000103");
}
