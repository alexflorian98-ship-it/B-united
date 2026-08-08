using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.Security;

/// <summary>
/// A minimal, real ASP.NET Core pipeline (JWT bearer auth + <c>AddIdentityPermissionPolicies</c>
/// wired exactly as <c>Program.cs</c> wires it) with one throwaway endpoint per permission key,
/// used only to prove the authentication/authorization middleware actually enforces each policy
/// end-to-end over real HTTP — not shipped in the real Api project.
/// </summary>
public sealed class PermissionTestHostFixture : IAsyncLifetime
{
    private static readonly IOptions<JwtOptions> JwtOptions = Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    });

    private IHost? _host;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();

                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = JwtOptions.Value.Issuer,
                        ["Jwt:Audience"] = JwtOptions.Value.Audience,
                        ["Jwt:SigningKey"] = JwtOptions.Value.SigningKey,
                    });
                });

                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddRouting();
                    services.AddIdentityJwtAuthentication(context.Configuration);
                    services.AddIdentityPermissionPolicies();
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        foreach (var (_, key, _) in WellKnownPermissions.All)
                        {
                            endpoints.MapGet($"/test/{key}", () => Results.Ok()).RequireAuthorization(key);
                        }

                        endpoints.MapGet("/test/authenticated-only", () => Results.Ok()).RequireAuthorization();
                    });
                });
            });

        _host = await hostBuilder.StartAsync();
        Client = _host.GetTestClient();
    }

    public string IssueToken(IReadOnlyCollection<string> permissionKeys) =>
        new JwtTokenGenerator(JwtOptions).GenerateAccessToken(Guid.NewGuid(), "permission-test@example.com", permissionKeys).Token;

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
