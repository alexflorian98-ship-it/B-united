using BUnited.BuildingBlocks.Security.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BUnited.BuildingBlocks.Security.Tests;

/// <summary>Security-gap-closure item #8 ("forwarded headers and trusted proxy configuration").
/// Proves the safe-by-default behavior (an unconfigured deployment ignores client-supplied
/// X-Forwarded-* headers rather than blindly trusting them) and that configuring a known proxy
/// IP makes the middleware apply those headers as documented.</summary>
public sealed class ForwardedHeadersExtensionsTests
{
    private static TestServer BuildServer(Dictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.Configure(app =>
            {
                app.UseBUnitedForwardedHeaders(configuration);
                app.Run(context => context.Response.WriteAsync(
                    $"{context.Connection.RemoteIpAddress}|{context.Request.Scheme}"));
            });
        });

        return builder.Start().GetTestServer();
    }

    /// <summary>Confirms the reason <see cref="ForwardedHeadersExtensions"/> skips registering
    /// the middleware entirely when unconfigured, instead of relying on
    /// <see cref="Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersOptions"/>'s own empty-list
    /// behavior: an empty KnownProxies/KnownNetworks tells the real ASP.NET Core middleware to
    /// skip proxy validation and apply ANY caller's forwarded headers unconditionally — the
    /// opposite of safe-by-default.</summary>
    [Fact]
    public async Task ForwardedHeadersMiddleware_with_empty_known_lists_applies_any_callers_header_unconditionally()
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.Configure(app =>
            {
                app.UseForwardedHeaders(options);
                app.Run(context => context.Response.WriteAsync(context.Connection.RemoteIpAddress?.ToString() ?? ""));
            });
        });

        using var server = builder.Start().GetTestServer();
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");

        var response = await client.GetStringAsync("/");

        Assert.Equal("203.0.113.99", response);
    }

    [Fact]
    public async Task With_no_known_proxies_configured_the_middleware_is_not_registered_and_a_spoofed_header_is_ignored()
    {
        using var server = BuildServer(new Dictionary<string, string?>());
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        var response = await client.GetStringAsync("/");

        // TestServer's default connecting peer, not the attacker-supplied header value.
        Assert.DoesNotContain("203.0.113.99", response);
        Assert.EndsWith("|http", response);
    }

    [Fact]
    public async Task With_the_connecting_peer_listed_as_a_known_proxy_the_forwarded_header_is_applied()
    {
        // TestServer's default connecting-peer IP.
        const string testServerPeerIp = "127.0.0.1";

        using var server = BuildServer(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = testServerPeerIp,
        });
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        var response = await client.GetStringAsync("/");

        Assert.StartsWith("203.0.113.99|", response);
        Assert.EndsWith("|https", response);
    }
}
