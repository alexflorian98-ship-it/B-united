using System.Text.Json;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.BuildingBlocks.Observability.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.BuildingBlocks.Observability.Tests;

public sealed class GlobalExceptionHandlerTests
{
    private sealed class StaticCorrelationIdAccessor(string correlationId) : ICorrelationIdAccessor
    {
        public string CorrelationId { get; } = correlationId;
    }

    private static (GlobalExceptionHandler Handler, DefaultHttpContext Context, MemoryStream Body) CreateHandler(
        string correlationId = "test-correlation-id")
    {
        // GlobalExceptionHandler is registered as a singleton (AddExceptionHandler<T> requirement)
        // and resolves the scoped ICorrelationIdAccessor from HttpContext.RequestServices per call
        // rather than via constructor injection — see the captive-dependency note on the type.
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var services = new ServiceCollection();
        services.AddSingleton<ICorrelationIdAccessor>(new StaticCorrelationIdAccessor(correlationId));

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        var body = new MemoryStream();
        context.Response.Body = body;

        return (handler, context, body);
    }

    private static async Task<ErrorResponse> ReadResponseAsync(MemoryStream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ErrorResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public async Task Unhandled_exception_maps_to_500_internal_server_error()
    {
        var (handler, context, body) = CreateHandler();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var response = await ReadResponseAsync(body);
        Assert.Equal("INTERNAL_SERVER_ERROR", response.Code);
        Assert.Equal("errors.internalServerError", response.MessageKey);
        Assert.Equal("test-correlation-id", response.CorrelationId);
        Assert.Null(response.Errors);
    }

    [Fact]
    public async Task NotFoundAppException_maps_to_404()
    {
        var (handler, context, body) = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new NotFoundAppException("Program 123 not found."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var response = await ReadResponseAsync(body);
        Assert.Equal("NOT_FOUND", response.Code);
        Assert.Equal("errors.notFound", response.MessageKey);
    }

    [Fact]
    public async Task BusinessRuleAppException_maps_to_400_with_its_own_code()
    {
        var (handler, context, body) = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new BusinessRuleAppException("SUBSCRIPTION_INACTIVE", "errors.subscriptionInactive", "Subscription is not active."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var response = await ReadResponseAsync(body);
        Assert.Equal("SUBSCRIPTION_INACTIVE", response.Code);
        Assert.Equal("errors.subscriptionInactive", response.MessageKey);
    }

    [Fact]
    public async Task ValidationException_maps_to_400_with_field_errors()
    {
        var (handler, context, body) = CreateHandler();
        var failures = new[]
        {
            new ValidationFailure("Email", "Email is required.") { ErrorCode = "errors.email.required" },
            new ValidationFailure("Password", "Password is too short.") { ErrorCode = "errors.password.tooShort" },
        };

        var handled = await handler.TryHandleAsync(context, new ValidationException(failures), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var response = await ReadResponseAsync(body);
        Assert.Equal("VALIDATION_FAILED", response.Code);
        Assert.NotNull(response.Errors);
        Assert.Equal(2, response.Errors!.Count);
        Assert.Contains(response.Errors, e => e.Field == "Email" && e.MessageKey == "errors.email.required");
        Assert.Contains(response.Errors, e => e.Field == "Password" && e.MessageKey == "errors.password.tooShort");
    }
}
