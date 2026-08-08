using System.Text.Json;
using System.Threading.RateLimiting;
using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.BuildingBlocks.Observability.ErrorHandling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.BuildingBlocks.Security.Abuse;

/// <summary>
/// Shared ASP.NET Core rate limiting setup: a global per-IP fixed-window policy for all
/// requests (health endpoints excluded) plus a stricter named <see cref="AuthPolicyName"/>
/// policy for authentication endpoints (login, password reset, etc. — apply via
/// <c>[EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]</c> once those endpoints
/// exist, see P1.18+). Rejections are written using the standard error contract shape
/// (docs/ARCHITECTURE.md §24: code/messageKey/correlationId).
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicyName = "auth";

    private const int GlobalPermitLimit = 100;
    private static readonly TimeSpan GlobalWindow = TimeSpan.FromMinutes(1);

    private const int AuthPermitLimit = 5;
    private static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddBUnitedRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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
                        PermitLimit = GlobalPermitLimit,
                        Window = GlobalWindow,
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy(AuthPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthPermitLimit,
                        Window = AuthWindow,
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
