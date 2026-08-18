using System.Text.Json;
using System.Threading.RateLimiting;
using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.BuildingBlocks.Observability.ErrorHandling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.BuildingBlocks.Security.Abuse;

/// <summary>
/// Shared ASP.NET Core rate limiting setup: a global per-IP fixed-window policy for all
/// requests (health endpoints excluded) plus a stricter named <see cref="AuthPolicyName"/>
/// policy for authentication endpoints (login, password reset, etc. — apply via
/// <c>[EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]</c> once those endpoints
/// exist, see P1.18+). Rejections are written using the standard error contract shape
/// (docs/ARCHITECTURE.md §24: code/messageKey/correlationId).
///
/// Limits are configuration-bound (see <see cref="RateLimitingOptions"/>) so the local
/// Development environment can grant a canonical multi-project Playwright run headroom above
/// the production auth budget without ever touching the production default: appsettings.json
/// (loaded in every environment, including Production) still defaults to the original 5
/// requests/minute, and only appsettings.Development.json raises it.
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicyName = "auth";

    public static IServiceCollection AddBUnitedRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            var rateLimiting = configuration
                .GetSection(RateLimitingOptions.SectionName)
                .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (httpContext.Request.Path.StartsWithSegments("/health"))
                {
                    return RateLimitPartition.GetNoLimiter("health");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimiting.Global.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimiting.Global.WindowSeconds),
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy(AuthPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimiting.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimiting.Auth.WindowSeconds),
                        QueueLimit = 0,
                    }));

            options.OnRejected = WriteRejectionResponseAsync;
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteRejectionResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.ContentType = "application/json";

        var correlationId = context.HttpContext.RequestServices
            .GetRequiredService<ICorrelationIdAccessor>()
            .CorrelationId;

        var payload = JsonSerializer.Serialize(
            ErrorResponse.RateLimitExceeded(correlationId),
            ErrorResponse.JsonOptions);

        await context.HttpContext.Response.WriteAsync(payload, cancellationToken);
    }
}
