using BUnited.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Registers one authorization policy per permission key in <see cref="WellKnownPermissions"/>,
/// each requiring the caller's JWT to carry a matching <c>permission</c> claim. Controllers
/// must use <c>[Authorize(Policy = WellKnownPermissions.ContentPublish)]</c> etc. — never
/// <c>if (user.Role == "Expert")</c> role-string checks (docs/DEVELOPMENT_INSTRUCTIONS.md §3).
/// </summary>
public static class IdentityAuthorizationExtensions
{
    public static IServiceCollection AddIdentityPermissionPolicies(this IServiceCollection services)
    {
        return services.AddAuthorization(options =>
        {
            foreach (var (_, key, _) in WellKnownPermissions.All)
            {
                options.AddPolicy(key, policy => policy.RequireClaim("permission", key));
            }
        });
    }
}
