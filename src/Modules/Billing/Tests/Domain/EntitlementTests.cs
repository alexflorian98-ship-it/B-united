using BUnited.Modules.Billing.Domain.Entities;

namespace BUnited.Modules.Billing.Tests.Domain;

public sealed class EntitlementTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Indefinite_entitlement_is_always_active()
    {
        var entitlement = Entitlement.Grant(Guid.NewGuid(), Entitlement.PlatformAccessType, UtcNow, null, Guid.NewGuid());
        Assert.True(entitlement.IsActiveAt(UtcNow.AddYears(10)));
    }

    [Fact]
    public void Entitlement_is_active_before_its_cutoff()
    {
        var entitlement = Entitlement.Grant(Guid.NewGuid(), Entitlement.PlatformAccessType, UtcNow, UtcNow.AddDays(3), Guid.NewGuid());
        Assert.True(entitlement.IsActiveAt(UtcNow.AddDays(2)));
    }

    [Fact]
    public void Entitlement_is_inactive_after_its_cutoff()
    {
        var entitlement = Entitlement.Grant(Guid.NewGuid(), Entitlement.PlatformAccessType, UtcNow, UtcNow.AddDays(3), Guid.NewGuid());
        Assert.False(entitlement.IsActiveAt(UtcNow.AddDays(4)));
    }

    [Fact]
    public void Revoked_entitlement_is_never_active()
    {
        var entitlement = Entitlement.Grant(Guid.NewGuid(), Entitlement.PlatformAccessType, UtcNow, null, Guid.NewGuid());
        entitlement.Revoke();
        Assert.False(entitlement.IsActiveAt(UtcNow));
    }

    [Fact]
    public void Extend_reactivates_a_revoked_entitlement()
    {
        var entitlement = Entitlement.Grant(Guid.NewGuid(), Entitlement.PlatformAccessType, UtcNow, null, Guid.NewGuid());
        entitlement.Revoke();
        entitlement.Extend(null);
        Assert.True(entitlement.IsActiveAt(UtcNow));
    }
}
