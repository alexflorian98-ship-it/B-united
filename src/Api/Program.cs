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
using BUnited.Modules.Admin.Infrastructure;
using BUnited.Modules.Audit.Infrastructure;
using BUnited.Modules.Billing.Api.Controllers;
using BUnited.Modules.Billing.Infrastructure;
using BUnited.Modules.Chat.Api.Controllers;
using BUnited.Modules.Chat.Infrastructure;
using BUnited.Modules.Content.Api.Controllers;
using BUnited.Modules.Content.Infrastructure;
using BUnited.Modules.Events.Api.Controllers;
using BUnited.Modules.Events.Application.Jobs;
using BUnited.Modules.Events.Infrastructure;
using BUnited.Modules.Identity.Api.Controllers;
using BUnited.Modules.Identity.Infrastructure;
using BUnited.Modules.Notifications.Infrastructure;
using BUnited.Modules.Progress.Api.Controllers;
using BUnited.Modules.Progress.Infrastructure;
using BUnited.Modules.Questionnaires.Api.Controllers;
using BUnited.Modules.Questionnaires.Infrastructure;
using Hangfire;
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
    .AddApplicationPart(typeof(BillingController).Assembly)
    .AddApplicationPart(typeof(EventsController).Assembly)
    .AddApplicationPart(typeof(ChatController).Assembly);
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
builder.Services.AddEventsModule(builder.Configuration);
builder.Services.AddChatModule();
builder.Services.AddAdminModule();

// P3.15/ADR-003: real IProgramAccessContext (per-program ownership, not a global subscription
// check) — Billing owns this decision. See BillingModuleExtensions for the registration.
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
    await ProgramOfferSeeder.SeedAsync(db);
}

// P5.08: idempotent 24h/1h event reminder sweep, every 5 minutes. Registered here (post-build,
// after migrations) rather than inside AddEventsModule, mirroring the seeders above — Hangfire's
// PostgreSQL storage auto-creates its own "hangfire" schema on first connection. Uses the
// DI-scoped IRecurringJobManager, not the static RecurringJob API — the DI-based AddHangfire()
// overload used here does not populate the legacy static JobStorage.Current.
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<SendDueEventRemindersHandler>(
    "send-due-event-reminders",
    handler => handler.HandleAsync(CancellationToken.None),
    "*/5 * * * *");

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
