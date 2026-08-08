namespace BUnited.BuildingBlocks.Application.Access;

/// <summary>
/// Billing exclusively owns subscription state and the <c>PlatformAccess</c> entitlement
/// (CLAUDE.md); every other module that needs to gate a feature on an active subscription
/// consumes this interface instead of referencing Billing's Domain/Infrastructure directly.
/// </summary>
public interface IAccessContext
{
    Task<bool> HasActivePlatformAccessAsync(Guid userId, CancellationToken cancellationToken);
}
