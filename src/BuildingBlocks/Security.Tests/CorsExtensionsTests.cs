using BUnited.BuildingBlocks.Security.Cors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BUnited.BuildingBlocks.Security.Tests;

/// <summary>Security-gap-closure item #8 ("rejection of wildcard origins", "no credentialed
/// wildcard CORS"). <see cref="CorsExtensions"/> never calls <c>AllowAnyOrigin</c> anywhere in
/// the codebase (confirmed by repo-wide search) — every policy is built from an explicit
/// <c>WithOrigins</c> allow-list read from <c>Cors:AllowedOrigins</c>. This proves that behavior
/// end to end: a literal "*" in the configured list does not enable wildcard matching (ASP.NET
/// Core's CORS middleware treats "*" from <c>WithOrigins</c> as a literal, never-matching string,
/// not a wildcard), an empty list allows nothing, and only an exact configured origin is allowed.</summary>
public sealed class CorsExtensionsTests
{
    private static TestServer BuildServer(string[] allowedOrigins)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(allowedOrigins.Select((origin, index) =>
                new KeyValuePair<string, string?>($"Cors:AllowedOrigins:{index}", origin)))
            .Build();

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services => services.AddBUnitedCors(configuration));
            webHost.Configure(app =>
            {
                app.UseCors(CorsExtensions.SpaPolicyName);
                app.Run(context => context.Response.WriteAsync("ok"));
            });
        });

        return builder.Start().GetTestServer();
    }

    private static async Task<HttpResponseMessage> SendPreflightAsync(TestServer server, string origin)
    {
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task A_literal_wildcard_in_configuration_does_not_enable_wildcard_matching()
    {
        using var server = BuildServer(["*"]);

        var response = await SendPreflightAsync(server, "https://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task An_empty_configured_list_allows_no_origin()
    {
        using var server = BuildServer([]);

        var response = await SendPreflightAsync(server, "https://app.example.com");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Only_the_exact_configured_origin_is_allowed()
    {
        using var server = BuildServer(["https://app.example.com"]);

        var allowed = await SendPreflightAsync(server, "https://app.example.com");
        Assert.Equal("https://app.example.com", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());

        var hostile = await SendPreflightAsync(server, "https://evil.example");
        Assert.False(hostile.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
