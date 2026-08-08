using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BUnited.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Maps the standard BUnited health endpoints, separating application liveness (is the
/// process responsive) from dependency readiness (are required dependencies, e.g. PostgreSQL,
/// reachable) per docs/DEVELOPMENT_INSTRUCTIONS.md §10. Dependency checks must be registered
/// with <see cref="ReadinessTag"/> via <c>AddHealthChecks()</c> at the composition root.
/// </summary>
public static class HealthCheckEndpointExtensions
{
    public const string ReadinessTag = "ready";

    public static IEndpointRouteBuilder MapBUnitedHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthCheckResponseAsync,
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteHealthCheckResponseAsync,
        });

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteHealthCheckResponseAsync,
        });

        return endpoints;
    }

    private static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        // Deliberately excludes HealthReportEntry.Exception/Description: those can surface
        // connection details or provider payloads and must never reach an API client (§6).
        var payload = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
            }),
        });

        return context.Response.WriteAsync(payload);
    }
}
