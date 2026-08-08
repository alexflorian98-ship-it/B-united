namespace BUnited.Modules.Identity.Contracts;

/// <summary>
/// Read-only cross-module lookup for admin/dashboard read models (CLAUDE.md: "Read-only
/// cross-module queries are allowed only in explicit admin/dashboard read models") — e.g. the
/// Questionnaires expert queue needs to show which client a submission belongs to without
/// Questionnaires referencing Identity's Domain/Infrastructure layers directly.
/// </summary>
public interface IUserLookup
{
    Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}

public sealed record UserSummary(Guid Id, string Email, string? DisplayName);
