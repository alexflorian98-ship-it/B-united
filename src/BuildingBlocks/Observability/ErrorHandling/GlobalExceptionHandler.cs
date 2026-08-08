using System.Text.Json;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.BuildingBlocks.Observability.CorrelationId;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BUnited.BuildingBlocks.Observability.ErrorHandling;

/// <summary>
/// Translates unhandled exceptions into the standard error contract (docs/ARCHITECTURE.md
/// §24). Never surfaces exception details, stack traces or provider payloads to the client
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §6) — full exception details are logged server-side only.
/// Register via <c>builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c> and
/// <c>app.UseExceptionHandler()</c>.
/// </summary>
/// <remarks>
/// <c>AddExceptionHandler&lt;T&gt;</c> registers <typeparamref name="GlobalExceptionHandler"/>
/// (self) as a <b>singleton</b>, so <see cref="ICorrelationIdAccessor"/> (scoped) must be
/// resolved per-invocation from <c>httpContext.RequestServices</c> rather than injected via the
/// constructor — constructor injection here would capture the first request's scoped instance
/// and leak it into every subsequent request's error responses.
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.RequestServices
            .GetRequiredService<ICorrelationIdAccessor>()
            .CorrelationId;

        var (statusCode, response) = Resolve(exception, correlationId);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request rejected with {Code}. CorrelationId: {CorrelationId}",
                response.Code,
                correlationId);
        }

        // The exception-handler middleware clears any response state set before the exception
        // was caught (including the X-Correlation-Id header set by CorrelationIdMiddleware), so
        // it must be re-applied here.
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, ErrorResponse.JsonOptions), cancellationToken);

        return true;
    }

    internal static (int StatusCode, ErrorResponse Response) Resolve(Exception exception, string correlationId) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                ErrorResponse.FromValidationFailures(validationException.Errors, correlationId)),
            NotFoundAppException notFoundException => (
                StatusCodes.Status404NotFound,
                ErrorResponse.FromAppException(notFoundException, correlationId)),
            AppException appException => (
                StatusCodes.Status400BadRequest,
                ErrorResponse.FromAppException(appException, correlationId)),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorResponse.InternalServerError(correlationId)),
        };
}
