using System.Security.Claims;
using BUnited.Modules.Admin.Infrastructure;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Admin.Tests.Application;

/// <summary>Proves the <see cref="AdminPermissionPolicies.DashboardView"/> policy registered by
/// <see cref="AdminModuleExtensions.AddAdminModule"/> grants access to any one of the four
/// module permissions the dashboard's widgets are gated on individually, and denies both an
/// anonymous caller and a caller holding an unrelated permission — the negative-authorization
/// cases docs/DEVELOPMENT_INSTRUCTIONS.md §9 requires for protected endpoints.</summary>
public sealed class AdminDashboardPolicyTests
{
    private static async Task<AuthorizationPolicy> ResolvePolicyAsync()
    {
        var services = new ServiceCollection();
        services.AddAdminModule();
        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await policyProvider.GetPolicyAsync(AdminPermissionPolicies.DashboardView);
        Assert.NotNull(policy);
        return policy!;
    }

    private static async Task<bool> EvaluateAsync(AuthorizationPolicy policy, ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IAuthorizationService>();
        var result = await evaluator.AuthorizeAsync(user, resource: null, policy);
        return result.Succeeded;
    }

    private static ClaimsPrincipal PrincipalWith(params string[] permissions) =>
        new(new ClaimsIdentity(permissions.Select(p => new Claim("permission", p)), authenticationType: "Test"));

    [Theory]
    [InlineData(WellKnownPermissionKeys.QuestionnaireReview)]
    [InlineData(WellKnownPermissionKeys.BillingManage)]
    [InlineData(WellKnownPermissionKeys.EventsManage)]
    [InlineData(WellKnownPermissionKeys.ChatModerate)]
    public async Task Grants_access_to_a_holder_of_any_qualifying_permission(string permission)
    {
        var policy = await ResolvePolicyAsync();

        var succeeded = await EvaluateAsync(policy, PrincipalWith(permission));

        Assert.True(succeeded);
    }

    [Fact]
    public async Task Denies_an_anonymous_caller()
    {
        var policy = await ResolvePolicyAsync();

        var succeeded = await EvaluateAsync(policy, new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(succeeded);
    }

    [Fact]
    public async Task Denies_a_caller_holding_only_an_unrelated_permission()
    {
        var policy = await ResolvePolicyAsync();

        var succeeded = await EvaluateAsync(policy, PrincipalWith(WellKnownPermissionKeys.ContentView));

        Assert.False(succeeded);
    }
}
