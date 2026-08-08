using BUnited.BuildingBlocks.Observability.CorrelationId;
using Microsoft.AspNetCore.Http;

namespace BUnited.BuildingBlocks.Observability.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Generates_a_new_correlation_id_when_header_is_absent()
    {
        var context = new DefaultHttpContext();
        var correlationIdContext = new CorrelationIdContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, correlationIdContext);

        Assert.False(string.IsNullOrWhiteSpace(correlationIdContext.CorrelationId));
        Assert.True(Guid.TryParse(correlationIdContext.CorrelationId, out _));
        Assert.Equal(
            correlationIdContext.CorrelationId,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Reuses_the_inbound_correlation_id_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "caller-supplied-id";
        var correlationIdContext = new CorrelationIdContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, correlationIdContext);

        Assert.Equal("caller-supplied-id", correlationIdContext.CorrelationId);
        Assert.Equal(
            "caller-supplied-id",
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Calls_the_next_middleware_in_the_pipeline()
    {
        var context = new DefaultHttpContext();
        var correlationIdContext = new CorrelationIdContext();
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, correlationIdContext);

        Assert.True(nextCalled);
    }
}
