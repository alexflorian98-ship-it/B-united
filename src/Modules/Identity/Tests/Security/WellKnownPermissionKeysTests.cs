using System.Reflection;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Identity.Domain;

namespace BUnited.Modules.Identity.Tests.Security;

/// <summary>Guards against <c>Identity.Contracts.WellKnownPermissionKeys</c> (the cross-module-
/// safe mirror other modules reference) drifting from <c>Identity.Domain.WellKnownPermissions</c>
/// (the canonical set actually registered as authorization policies).</summary>
public sealed class WellKnownPermissionKeysTests
{
    [Fact]
    public void WellKnownPermissionKeys_mirrors_the_domain_permission_set_exactly()
    {
        var domainKeys = WellKnownPermissions.All.Select(p => p.Key).OrderBy(k => k).ToArray();

        var contractKeys = typeof(WellKnownPermissionKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(k => k)
            .ToArray();

        Assert.Equal(domainKeys, contractKeys);
    }
}
