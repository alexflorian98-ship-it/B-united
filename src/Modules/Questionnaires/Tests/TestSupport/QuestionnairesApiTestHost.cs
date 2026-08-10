using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Questionnaires.Api.Controllers;
using BUnited.Modules.Questionnaires.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Questionnaires.Tests.TestSupport;

/// <summary>
/// A minimal, real ASP.NET Core pipeline hosting the actual <see cref="ExpertQuestionnairesController"/>
/// behind the real JWT bearer authentication + permission-policy middleware (mirrors
/// <c>Identity.Tests.Security.PermissionTestHostFixture</c>, applied to a real module controller
/// instead of throwaway endpoints) — used to prove P4.33.a end-to-end over real HTTP: that the
/// <c>[Authorize(Policy = WellKnownPermissionKeys.QuestionnaireReview)]</c> attribute on the
/// submission-detail endpoint actually denies a caller who lacks that specific permission claim,
/// including a caller holding every other Administrator-role permission. Not shipped in the real
/// Api project — test-only composition root.
/// </summary>
internal sealed class QuestionnairesApiTestHost : IAsyncDisposable
{
    private static readonly IOptions<JwtOptions> JwtOptions = Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    });

    private readonly SqliteConnection _connection;
    private readonly IHost _host;

    public TestDbContext DbContext { get; }

    public HttpClient Client { get; }

    public FakeAuditLogger AuditLogger { get; }

    private QuestionnairesApiTestHost(SqliteConnection connection, TestDbContext dbContext, FakeAuditLogger auditLogger, IHost host)
    {
        _connection = connection;
        DbContext = dbContext;
        AuditLogger = auditLogger;
        _host = host;
        Client = host.GetTestClient();
    }

    public static async Task<QuestionnairesApiTestHost> StartAsync()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        var auditLogger = new FakeAuditLogger();
        var userLookup = new FakeUserLookup();
        var notificationSender = new FakeNotificationSender();

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
                    services.AddSingleton<DbContext>(dbContext);
                    services.AddSingleton(TimeProvider.System);
                    services.AddSingleton<Identity.Contracts.IUserLookup>(userLookup);
                    services.AddSingleton<Audit.Contracts.IAuditLogger>(auditLogger);
                    services.AddSingleton<Notifications.Contracts.INotificationSender>(notificationSender);
                    services.AddQuestionnairesModule();

                    services.AddIdentityJwtAuthentication(context.Configuration);
                    services.AddIdentityPermissionPolicies();

                    services.AddRouting();
                    services.AddControllers().AddApplicationPart(typeof(ExpertQuestionnairesController).Assembly);
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            });

        var host = await hostBuilder.StartAsync();
        return new QuestionnairesApiTestHost(connection, dbContext, auditLogger, host);
    }

    public string IssueToken(IReadOnlyCollection<string> permissionKeys) =>
        new JwtTokenGenerator(JwtOptions).GenerateAccessToken(Guid.NewGuid(), "permission-test@example.com", permissionKeys).Token;

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        DbContext.Dispose();
        _connection.Dispose();
    }
}
