using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.BuildingBlocks.Observability.ErrorHandling;
using BUnited.Modules.Billing.Api.Controllers;
using BUnited.Modules.Billing.Infrastructure;
using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Identity.Application.Configuration;
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

namespace BUnited.Modules.Billing.Tests.TestSupport;

/// <summary>
/// A minimal, real ASP.NET Core pipeline hosting the actual <see cref="BillingController"/> behind
/// the real JWT bearer authentication + permission-policy middleware (mirrors
/// <c>Questionnaires.Tests.TestSupport.QuestionnairesApiTestHost</c>) — used for P3.30's HTTP-level
/// cross-user invoice-access denial test: proves the real controller/routing/DI wiring, not just
/// <see cref="Application.UseCases.GetMyInvoiceHandler"/> in isolation, resolves
/// <c>GET my-invoices/{invoiceId}</c> from the caller's own JWT <c>sub</c> claim and denies another
/// user's invoice with 404. Not shipped in the real Api project — test-only composition root.
/// </summary>
internal sealed class BillingApiTestHost : IAsyncDisposable
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

    public FakeProgramLookup ProgramLookup { get; }

    private BillingApiTestHost(SqliteConnection connection, TestDbContext dbContext, FakeProgramLookup programLookup, IHost host)
    {
        _connection = connection;
        DbContext = dbContext;
        ProgramLookup = programLookup;
        _host = host;
        Client = host.GetTestClient();
    }

    public static async Task<BillingApiTestHost> StartAsync()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();
        var auditLogger = new FakeAuditLogger();
        var programLookup = new FakeProgramLookup();

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
                    services.AddSingleton<Audit.Contracts.IAuditLogger>(auditLogger);
                    services.AddSingleton<IProgramLookup>(programLookup);
                    services.AddBillingModule(context.Configuration);

                    services.AddIdentityJwtAuthentication(context.Configuration);
                    services.AddIdentityPermissionPolicies();

                    services.AddCorrelationId();
                    services.AddBUnitedErrorHandling();

                    services.AddRouting();
                    services.AddControllers().AddApplicationPart(typeof(BillingController).Assembly);
                });

                webBuilder.Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseCorrelationId();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            });

        var host = await hostBuilder.StartAsync();
        return new BillingApiTestHost(connection, dbContext, programLookup, host);
    }

    public string IssueToken(Guid userId, IReadOnlyCollection<string> permissionKeys) =>
        new JwtTokenGenerator(JwtOptions).GenerateAccessToken(userId, $"{userId:N}@example.com", permissionKeys).Token;

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        DbContext.Dispose();
        _connection.Dispose();
    }
}
