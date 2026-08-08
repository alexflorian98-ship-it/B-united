using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BUnited.BuildingBlocks.Observability.CorrelationId;

/// <summary>
/// Reads the inbound <c>X-Correlation-Id</c> header (or generates one), stores it on the
/// request-scoped <see cref="CorrelationIdContext"/>, echoes it back on the response, and
/// pushes it into the Serilog <see cref="LogContext"/> so every log line for this request
/// carries it automatically.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CorrelationIdContext correlationIdContext)
    {
        var correlationId = ResolveCorrelationId(context);

        correlationIdContext.CorrelationId = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
