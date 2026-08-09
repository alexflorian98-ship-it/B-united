namespace BUnited.Modules.Identity.Contracts;

/// <summary>
/// Records and checks versioned user consent (<c>UserConsent</c>, defined in Phase 1 P1.15 but
/// unused until now — the same "define now, wire up when a real caller exists" pattern as
/// P1.23.c/P1.30.b/P1.33's role-assignment gap). Other modules consume this instead of
/// referencing Identity's Domain/Infrastructure directly, mirroring <c>IProgramAccessContext</c>.
/// </summary>
public interface IConsentContext
{
    Task<bool> HasConsentedAsync(Guid userId, string consentType, int requiredVersion, CancellationToken cancellationToken);

    Task RecordConsentAsync(Guid userId, string consentType, int version, CancellationToken cancellationToken);
}
