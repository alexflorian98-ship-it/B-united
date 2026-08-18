using BUnited.BuildingBlocks.Security.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace BUnited.BuildingBlocks.Security.Tests;

/// <summary>
/// Exercises <see cref="SecurityHeadersMiddleware"/> through a real ASP.NET Core pipeline
/// (<see cref="TestServer"/>) rather than invoking it directly: the middleware relies on
/// <c>Response.OnStarting</c>, which only fires once the host's response-body feature actually
/// starts the response — behavior a bare <see cref="DefaultHttpContext"/> does not provide.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    private static TestServer BuildServer(int responseStatusCode)
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.Configure(app =>
            {
                app.UseBUnitedSecurityHeaders();
                app.Run(context =>
                {
                    context.Response.StatusCode = responseStatusCode;
                    return Task.CompletedTask;
                });
            });
        });

        return builder.Start().GetTestServer();
    }

    [Fact]
    public async Task Adds_defensive_headers_on_a_successful_response()
    {
        using var server = BuildServer(StatusCodes.Status200OK);
        using var client = server.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("camera=(), microphone=(), geolocation=()", response.Headers.GetValues("Permissions-Policy").Single());
        Assert.Equal("same-origin", response.Headers.GetValues("Cross-Origin-Resource-Policy").Single());
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
    }

    // Covers the audit's confirmed finding (docs/E2E_AUDIT_RESULT.md, 2026-08-18): an unknown
    // route's 404 was missing X-Content-Type-Options. These statuses are exactly the ones the
    // task requires coverage for: validation errors (400), auth rejection (401/403), routing
    // misses (404), unhandled exceptions mapped by the global exception handler (500), and
    // rate-limiter rejections (429).
    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status429TooManyRequests)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    public async Task Adds_nosniff_on_error_and_rejection_responses(int statusCode)
    {
        using var server = BuildServer(statusCode);
        using var client = server.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal((System.Net.HttpStatusCode)statusCode, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Calls_the_next_middleware_in_the_pipeline()
    {
        var nextCalled = false;
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.Configure(app =>
            {
                app.UseBUnitedSecurityHeaders();
                app.Run(context =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                });
            });
        });

        using var server = builder.Start().GetTestServer();
        using var client = server.CreateClient();

        await client.GetAsync("/");

        Assert.True(nextCalled);
    }
}
