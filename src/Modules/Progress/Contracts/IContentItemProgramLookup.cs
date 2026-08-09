namespace BUnited.Modules.Progress.Contracts;

/// <summary>
/// Read-only cross-module lookup so Progress can resolve the Program that owns a given
/// <c>ContentItemId</c>/<c>SectionId</c> without ever referencing Content's Domain or
/// Infrastructure layers directly (CLAUDE.md: "Never reference another module's Domain or
/// Infrastructure layer"). Implemented by Content's Infrastructure/CrossModule layer — mirrors
/// Content's own <c>IProgramLookup</c>/Identity's <c>IUserLookup</c> pattern. Progress's
/// progress-tracking handlers use this to resolve the owning program before deferring to
/// <c>IProgramAccessContext</c> (ADR-003 per-program entitlement).
/// </summary>
public interface IContentItemProgramLookup
{
    Task<Guid?> GetOwningProgramIdForContentItemAsync(Guid contentItemId, CancellationToken cancellationToken);

    Task<Guid?> GetOwningProgramIdForSectionAsync(Guid sectionId, CancellationToken cancellationToken);
}
