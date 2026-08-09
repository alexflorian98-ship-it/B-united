using BUnited.BuildingBlocks.Application.Errors;

namespace BUnited.BuildingBlocks.Application.Access;

/// <summary>Stable error codes owned by <see cref="IProgramAccessContext"/> — every module that
/// gates a feature on per-program ownership references these constants instead of inlining
/// literals at each call site (docs/DEVELOPMENT_INSTRUCTIONS.md §3).</summary>
public static class ProgramAccessErrorCodes
{
    public const string ProgramAccessRequired = "PROGRAM_ACCESS_REQUIRED";

    public const string ProgramAlreadyOwned = "PROGRAM_ALREADY_OWNED";
}

/// <summary>
/// Billing exclusively owns purchase state and the per-program <c>ProgramEntitlement</c>
/// (CLAUDE.md, ADR-003); every other module that needs to gate a feature on owning a specific
/// program consumes this interface instead of referencing Billing's Domain/Infrastructure
/// directly. Replaces the old global <c>IAccessContext.HasActivePlatformAccessAsync</c> check —
/// access is now scoped to a single <c>ProgramId</c>, never platform-wide.
/// </summary>
public interface IProgramAccessContext
{
    Task<bool> HasProgramAccessAsync(Guid userId, Guid programId, CancellationToken cancellationToken);

    /// <summary>Throws a <see cref="BusinessRuleAppException"/> with
    /// <see cref="ProgramAccessErrorCodes.ProgramAccessRequired"/> when the user does not own the
    /// program. A single canonical implementation (default interface method) so every consumer
    /// gets the identical error code/message-key pairing rather than re-implementing it.</summary>
    async Task RequireProgramAccessAsync(Guid userId, Guid programId, CancellationToken cancellationToken)
    {
        var hasAccess = await HasProgramAccessAsync(userId, programId, cancellationToken);
        if (!hasAccess)
        {
            throw new BusinessRuleAppException(
                ProgramAccessErrorCodes.ProgramAccessRequired,
                "errors.programAccessRequired",
                "Access to this program is required.");
        }
    }

    /// <summary>Batch form of <see cref="HasProgramAccessAsync"/> for callers resolving ownership for
    /// many programs at once (e.g. the client catalogue list) — avoids one round trip per program.
    /// The default implementation preserves correctness for any existing implementer by falling back
    /// to per-program calls; <c>BillingProgramAccessContext</c> overrides it with a single batched
    /// query.</summary>
    async Task<IReadOnlySet<Guid>> GetAccessibleProgramIdsAsync(
        Guid userId, IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken)
    {
        var result = new HashSet<Guid>();
        foreach (var programId in programIds)
        {
            if (await HasProgramAccessAsync(userId, programId, cancellationToken))
            {
                result.Add(programId);
            }
        }

        return result;
    }
}
