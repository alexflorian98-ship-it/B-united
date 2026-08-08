using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.BuildingBlocks.Security.Cors;

/// <summary>
/// The SPA calls the Api from a different origin (Vite dev server / the deployed frontend
/// host), so this must be explicit rather than relying on same-origin defaults. Reads trusted
/// origins from <c>Cors:AllowedOrigins</c> — an empty/missing list means the policy allows
/// nothing, never a wildcard (docs/DEVELOPMENT_INSTRUCTIONS.md §6: "CORS MUST use explicit
/// trusted origins in production. Wildcard origins with credentials are forbidden.").
/// </summary>
public static class CorsExtensions
{
    public const string SpaPolicyName = "spa";

    public static IServiceCollection AddBUnitedCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(SpaPolicyName, policy => ConfigurePolicy(policy, allowedOrigins));
        });

        return services;
    }

    private static void ConfigurePolicy(CorsPolicyBuilder policy, string[] allowedOrigins)
    {
        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}
