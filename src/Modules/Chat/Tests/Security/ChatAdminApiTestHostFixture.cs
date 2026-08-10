using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Chat.Api.Controllers;
using BUnited.Modules.Chat.Infrastructure;
using BUnited.Modules.Chat.Tests.TestSupport;
using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Chat.Tests.Security;

/// <summary>P6.20.a — hosts the real <see cref="AdminChatController"/> (with its class-level
/// <c>[Authorize(Policy = WellKnownPermissionKeys.ChatModerate)]</c>) behind the real JWT/permission
/// authorization pipeline (mirroring Identity.Tests' <c>PermissionTestHostFixture</c> pattern), so a
/// wiring mistake on the actual controller — not just the generic policy middleware — would fail
/// this test. Application handler dependencies are the same fakes used by the module's other
/// handler-level tests (<see cref="FakeAuditLogger"/>, <see cref="FakeUserLookup"/>,
/// <see cref="FakeProgramLookup"/>).</summary>
public sealed class ChatAdminApiTestHostFixture : IAsyncLifetime
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
    private SqliteConnection? _connection;
    private DbContextOptions<TestDbContext>? _dbOptions;
    private readonly FakeProgramLookup _programLookup = new();

    public HttpClient Client { get; private set; } = null!;

    /// <summary>A fresh context over the same shared in-memory connection used by the host — the
    /// host's own request-scoped context is disposed by the DI container at the end of each
    /// request, so assertions after an HTTP call must read through a separate instance.</summary>
    public DbContext OpenAssertionContext() => new TestDbContext(_dbOptions!);

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor())
            .Options;

        await using (var seedContext = new TestDbContext(_dbOptions))
        {
            await seedContext.Database.EnsureCreatedAsync();
        }

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
                    services.AddIdentityJwtAuthentication(context.Configuration);
                    services.AddIdentityPermissionPolicies();

                    services.AddScoped<DbContext>(_ => new TestDbContext(_dbOptions!));
                    services.AddSingleton<IAuditLogger, FakeAuditLogger>();
                    services.AddSingleton<IUserLookup, FakeUserLookup>();
                    services.AddSingleton<IProgramLookup>(_programLookup);
                    services.AddChatModule();

                    services.AddControllers().AddApplicationPart(typeof(AdminChatController).Assembly);
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            });

        _host = await hostBuilder.StartAsync();
        Client = _host.GetTestClient();
    }

    public string IssueToken(IReadOnlyCollection<string> permissionKeys) =>
        new JwtTokenGenerator(JwtOptions).GenerateAccessToken(Guid.NewGuid(), "chat-test@example.com", permissionKeys).Token;

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
