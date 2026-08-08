using System.Text.Json;
using BUnited.BuildingBlocks.Application.Errors;
using FluentValidation.Results;

namespace BUnited.BuildingBlocks.Observability.ErrorHandling;

/// <summary>
/// The standard API error response shape (docs/ARCHITECTURE.md §24). <see cref="Errors"/> is
/// populated only for validation failures, per §61's field-level <c>errors[]</c> array.
/// Serialize with <see cref="JsonOptions"/> so the wire format matches §24's documented
/// camelCase example (<c>code</c>/<c>messageKey</c>/<c>correlationId</c>).
/// </summary>
public sealed record ErrorResponse(
    string Code,
    string MessageKey,
    string CorrelationId,
    IReadOnlyCollection<FieldErrorResponse>? Errors = null)
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ErrorResponse InternalServerError(string correlationId) =>
        new("INTERNAL_SERVER_ERROR", "errors.internalServerError", correlationId);

    public static ErrorResponse RateLimitExceeded(string correlationId) =>
        new("RATE_LIMIT_EXCEEDED", "errors.rateLimitExceeded", correlationId);

    public static ErrorResponse FromAppException(AppException exception, string correlationId) =>
        new(exception.Code, exception.MessageKey, correlationId);

    /// <summary>
    /// Maps FluentValidation failures produced by <see cref="FluentValidationActionFilter"/>.
    /// By convention, validators must set <c>WithErrorCode("errors.field....")</c> to a
    /// localization key — <see cref="ValidationFailure.ErrorCode"/> becomes the field's
    /// <see cref="FieldErrorResponse.MessageKey"/>; <see cref="ValidationFailure.ErrorMessage"/>
    /// is a developer-facing fallback and is never sent to the client.
    /// </summary>
    public static ErrorResponse FromValidationFailures(
        IEnumerable<ValidationFailure> failures,
        string correlationId) =>
        new(
            "VALIDATION_FAILED",
            "errors.validationFailed",
            correlationId,
            failures
                .Select(failure => new FieldErrorResponse(
                    failure.PropertyName,
                    string.IsNullOrWhiteSpace(failure.ErrorCode) ? "errors.validationFailed" : failure.ErrorCode))
                .ToArray());
}

public sealed record FieldErrorResponse(string Field, string MessageKey);
