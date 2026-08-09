namespace BUnited.BuildingBlocks.Application.DataRights;

/// <summary>
/// Cross-module contract for the GDPR-style "export my data" right (docs/PROMPT.md §66,
/// docs/DATA_RETENTION_POLICY.md). Mirrors the existing <c>IUserLookup</c>/<c>IProgramLookup</c>
/// read-only cross-module contract pattern: each module that owns user-scoped data implements
/// this in its own Application/Infrastructure layer and registers it in its module extension
/// method, so the orchestrating export handler (Identity) never references another module's
/// Domain or Infrastructure layer directly (CLAUDE.md). Resolved via DI as
/// <c>IEnumerable&lt;IUserDataExporter&gt;</c>.
/// </summary>
public interface IUserDataExporter
{
    /// <summary>Stable section key used as the top-level key in the exported JSON archive
    /// (e.g. "identity", "progress", "questionnaires", "billing", "events", "chat"). MUST be
    /// unique across all registered exporters.</summary>
    string SectionKey { get; }

    /// <summary>Returns this module's data owned by <paramref name="userId"/>, or null/empty if
    /// there is none. Read-only; MUST NOT mutate state. Implementations MUST scope every query
    /// strictly by <paramref name="userId"/> ownership — this is the caller's own data only,
    /// never gated by an admin-supplied identifier.</summary>
    Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken);
}
