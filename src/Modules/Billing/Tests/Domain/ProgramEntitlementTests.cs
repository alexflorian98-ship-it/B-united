using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;

namespace BUnited.Modules.Billing.Tests.Domain;

public sealed class ProgramEntitlementTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Granted_entitlement_is_active_with_no_expiration_concept()
    {
        var entitlement = ProgramEntitlement.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), UtcNow);

        Assert.True(entitlement.IsActive);
        Assert.Equal(ProgramEntitlementStatus.Active, entitlement.Status);
        Assert.Null(entitlement.RevokedAtUtc);

        // No expiration field/logic at all — access stays active arbitrarily far into the
        // future, unlike the old time-windowed Entitlement.
        Assert.True(entitlement.IsActive);
    }

    [Fact]
    public void Revoke_flips_status_and_stamps_revoked_at()
    {
        var entitlement = ProgramEntitlement.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), UtcNow);
        entitlement.Revoke("refund", UtcNow.AddDays(1));

        Assert.False(entitlement.IsActive);
        Assert.Equal(ProgramEntitlementStatus.Revoked, entitlement.Status);
        Assert.Equal(UtcNow.AddDays(1), entitlement.RevokedAtUtc);
    }

    [Fact]
    public void Revoking_an_already_revoked_entitlement_is_an_idempotent_no_op()
    {
        var entitlement = ProgramEntitlement.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), UtcNow);
        entitlement.Revoke("refund", UtcNow.AddDays(1));
        entitlement.Revoke("chargeback", UtcNow.AddDays(2));

        // The first revocation timestamp is preserved — a second revoke call is a no-op.
        Assert.Equal(UtcNow.AddDays(1), entitlement.RevokedAtUtc);
    }

    [Fact]
    public void Reactivate_restores_access_on_the_same_row()
    {
        var sourcePurchaseId = Guid.NewGuid();
        var entitlement = ProgramEntitlement.Grant(Guid.NewGuid(), Guid.NewGuid(), sourcePurchaseId, UtcNow);
        entitlement.Revoke("refund", UtcNow.AddDays(1));

        var newPurchaseId = Guid.NewGuid();
        entitlement.Reactivate(newPurchaseId, UtcNow.AddDays(10));

        Assert.True(entitlement.IsActive);
        Assert.Null(entitlement.RevokedAtUtc);
        Assert.Equal(newPurchaseId, entitlement.SourcePurchaseId);
        Assert.Equal(UtcNow.AddDays(10), entitlement.GrantedAtUtc);
    }
}
