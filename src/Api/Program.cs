using BUnited.Api.OpenApi;
using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.BuildingBlocks.Infrastructure.Safety;
using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.BuildingBlocks.Observability.ErrorHandling;
using BUnited.BuildingBlocks.Observability.HealthChecks;
using BUnited.BuildingBlocks.Observability.Logging;
using BUnited.BuildingBlocks.Security.Abuse;
using BUnited.BuildingBlocks.Security.Cors;
using BUnited.Migrations;
using BUnited.Migrations.Seed;
using BUnited.Modules.Audit.Infrastructure;
using BUnited.Modules.Billing.Api.Controllers;
using BUnited.Modules.Billing.Infrastructure;
using BUnited.Modules.Content.Api.Controllers;
using BUnited.Modules.Content.Infrastructure;
using BUnited.Modules.Identity.Api.Controllers;
using BUnited.Modules.Identity.Infrastructure;
using BUnited.Modules.Notifications.Infrastructure;
using BUnited.Modules.Progress.Api.Controllers;
using BUnited.Modules.Progress.Infrastructure;
using BUnited.Modules.Questionnaires.Api.Controllers;
using BUnited.Modules.Questionnaires.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Default is not configured. Set the ConnectionStrings__Default " +
        "environment variable (see .env.example) before starting the Api host.");
}

builder.AddBUnitedLogging();

// Add services to the container.

builder.Services.AddCorrelationId();
builder.Services.AddBUnitedErrorHandling();
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationActionFilter>())
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(AdminContentController).Assembly)
    .AddApplicationPart(typeof(ProgressController).Assembly)
    .AddApplicationPart(typeof(QuestionnairesController).Assembly)
    .AddApplicationPart(typeof(BillingController).Assembly);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeDocumentTransformer>();
    options.AddOperationTransformer<BearerSecurityRequirementOperationTransformer>();
});

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: [HealthCheckEndpointExtensions.ReadinessTag]);

builder.Services.AddBUnitedRateLimiting();
builder.Services.AddBUnitedCors(builder.Configuration);

builder.Services.AddDbContext<BUnitedApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(BUnitedApplicationDbContext).Assembly.FullName))
        .AddInterceptors(new AuditableEntitySaveChangesInterceptor()));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<BUnitedApplicationDbContext>());

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAuditModule();
builder.Services.AddContentModule();
builder.Services.AddProgressModule();
builder.Services.AddQuestionnairesModule();
builder.Services.AddNotificationsModule();

// P3.15: real IAccessContext, replacing the P2.09 StubAccessContext (removed — Billing now owns
// this decision). See BillingModuleExtensions for the registration.
builder.Services.AddBillingModule(builder.Configuration);

// P3.32: refuse to start in Production with any demo-only adapter (FakePaymentProvider,
// LoggingNotificationSender, LoggingIdentityEmailSender) registered — see ADR-010.
builder.Services.VerifyNoDemoOnlyAdaptersInProduction(builder.Environment);

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    var db = startupScope.ServiceProvider.GetRequiredService<BUnitedApplicationDbContext>();
    await db.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(db);
    await ContentSeeder.SeedAsync(db);
    await BillingSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
// Swagger UI is intentionally scoped to Development only (see docs/DEVELOPMENT_INSTRUCTIONS.md §4:
// OpenAPI contracts must match runtime behavior, but the interactive UI itself is not exposed
// in staging/production).
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BUnited API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseExceptionHandler();
app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseCors(CorsExtensions.SpaPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBUnitedHealthChecks();

app.Run();
