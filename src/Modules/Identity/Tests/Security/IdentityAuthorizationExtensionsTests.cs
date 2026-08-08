using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Identity.Tests.Security;

public sealed class IdentityAuthorizationExtensionsTests
{
    [Fact]
    public async Task Registers_one_policy_per_well_known_permission_key()
    {
        var services = new ServiceCollection();
        services.AddIdentityPermissionPolicies();
        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var (_, key, _) in WellKnownPermissions.All)
        {
            var policy = await policyProvider.GetPolicyAsync(key);
            Assert.NotNull(policy);
            Assert.Contains(
                policy!.Requirements.OfType<ClaimsAuthorizationRequirement>(),
                r => r.ClaimType == "permission" && r.AllowedValues!.Contains(key));
        }
    }
}
