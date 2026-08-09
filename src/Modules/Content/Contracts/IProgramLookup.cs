namespace BUnited.Modules.Content.Contracts;

/// <summary>
/// Read-only cross-module lookup so another module (Billing's admin offer management, P3.35)
/// can validate a <c>ProgramId</c> and check whether it is published without ever referencing
/// Content's Domain or Infrastructure layers directly (CLAUDE.md: "Never reference another
/// module's Domain or Infrastructure layer"). Mirrors Identity's <c>IUserLookup</c> pattern.
/// </summary>
public interface IProgramLookup
{
    Task<ProgramSummary?> GetProgramAsync(Guid programId, CancellationToken cancellationToken);
}

/// <summary><see cref="Status"/> mirrors Content's <c>ContentStatus</c> domain enum but is
/// declared independently here — Contracts must never reference another module's Domain layer,
/// so the mapping is done once, in the Infrastructure-layer implementation.</summary>
public enum ProgramLookupStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

/// <summary><paramref name="Title"/> is the program's title in its default language — optional
/// (defaulted, not a breaking addition for existing callers) because most existing callers only
/// need identity/status. Added for docs/IMPLEMENTATION_PLAN.md Slice A5, which snapshots it onto
/// <c>Purchase</c> at checkout time so historical billing records keep a readable label even if
/// the program is later renamed, unpublished, or archived.</summary>
public sealed record ProgramSummary(Guid Id, string Slug, ProgramLookupStatus Status, string DefaultLanguage, string? Title = null);
