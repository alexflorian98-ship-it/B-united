using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace BUnited.BuildingBlocks.Security.Proxy;

/// <summary>
/// Security-gap-closure item #8: reads which reverse proxies/networks are allowed to set
/// <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> from <c>ForwardedHeaders:KnownProxies</c> and
/// <c>ForwardedHeaders:KnownNetworks</c> (comma-separated IPs / CIDR networks).
///
/// Safe by default via a deliberate design choice, NOT via <see cref="ForwardedHeadersOptions"/>'s
/// own empty-list semantics: an empty <c>KnownProxies</c>/<c>KnownNetworks</c> tells ASP.NET
/// Core's <see cref="ForwardedHeadersMiddleware"/> to skip proxy validation entirely and apply
/// ANY caller's forwarded headers unconditionally — the opposite of "safe by default," and a
/// spoofable IP/scheme if this middleware were registered with nothing configured (confirmed via
/// <c>ForwardedHeadersExtensionsTests</c>). So instead: with nothing configured, this method does
/// not register the middleware at all — a true no-op, not a relied-upon empty-list behavior.
/// Configure the real reverse proxy's IP/network once the production topology is known — see
/// docs/security/PRODUCTION_VERIFICATION_RUNBOOK.md.
/// </summary>
public static class ForwardedHeadersExtensions
{
    public static IApplicationBuilder UseBUnitedForwardedHeaders(this IApplicationBuilder app, IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
        {
            var parts = network.Split('/');
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
            }
        }

        if (options.KnownProxies.Count == 0 && options.KnownNetworks.Count == 0)
        {
            return app;
        }

        return app.UseForwardedHeaders(options);
    }
}
