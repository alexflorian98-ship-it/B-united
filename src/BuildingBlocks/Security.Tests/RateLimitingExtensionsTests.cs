using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.BuildingBlocks.Security.Abuse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BUnited.BuildingBlocks.Security.Tests;

/// <summary>Proves the auth rate-limit budget stays at the production default (5/min) when no
/// override is configured — as it always is for appsettings.json/Production — and can only be
/// raised by an explicit configuration section, which is exactly what
/// appsettings.Development.json does to give the local canonical Playwright run headroom above
/// its 5-login floor without ever touching the production budget.</summary>
public sealed class RateLimitingExtensionsTests
{
    private static TestServer BuildServer(IEnumerable<KeyValuePair<string, string?>>? overrides = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(overrides ?? [])
            .Build();

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddCorrelationId();
                services.AddBUnitedRateLimiting(configuration);
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseRateLimiter();
                app.UseEndpoints(endpoints =>
                    endpoints.MapGet("/auth/probe", () => Results.Ok())
                        .RequireRateLimiting(RateLimitingExtensions.AuthPolicyName));
            });
        });

        return builder.Start().GetTestServer();
    }

    [Fact]
    public async Task Default_configuration_permits_exactly_five_auth_requests_per_window()
    {
        using var server = BuildServer();
        using var client = server.CreateClient();

        var statuses = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            using var response = await client.GetAsync("/auth/probe");
            statuses.Add((int)response.StatusCode);
        }

        Assert.Equal(5, statuses.Count(status => status == StatusCodes.Status200OK));
        Assert.Equal(1, statuses.Count(status => status == StatusCodes.Status429TooManyRequests));
    }

    [Fact]
    public async Task Configured_override_raises_the_auth_budget_without_a_code_change()
    {
        using var server = BuildServer([
            new KeyValuePair<string, string?>("RateLimiting:Auth:PermitLimit", "8"),
            new KeyValuePair<string, string?>("RateLimiting:Auth:WindowSeconds", "60"),
        ]);
        using var client = server.CreateClient();

        var statuses = new List<int>();
        for (var i = 0; i < 9; i++)
        {
            using var response = await client.GetAsync("/auth/probe");
            statuses.Add((int)response.StatusCode);
        }

        Assert.Equal(8, statuses.Count(status => status == StatusCodes.Status200OK));
        Assert.Equal(1, statuses.Count(status => status == StatusCodes.Status429TooManyRequests));
    }
}
