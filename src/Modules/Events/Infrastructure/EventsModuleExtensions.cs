using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Events.Application.Jobs;
using BUnited.Modules.Events.Application.UseCases.Admin;
using BUnited.Modules.Events.Application.UseCases.Client;
using BUnited.Modules.Events.Application.UseCases.DataRights;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Events.Infrastructure;

public static class EventsModuleExtensions
{
    public static IServiceCollection AddEventsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Admin
        services.AddScoped<CreateEventHandler>();
        services.AddScoped<UpsertEventTranslationHandler>();
        services.AddScoped<UpdateEventScheduleHandler>();
        services.AddScoped<EventStatusHandler>();
        services.AddScoped<ListEventsHandler>();
        services.AddScoped<GetEventDetailAdminHandler>();
        services.AddScoped<SetEventProgramsHandler>();

        // Client
        services.AddScoped<ListPublishedEventsHandler>();
        services.AddScoped<GetPublishedEventDetailHandler>();
        services.AddScoped<RegisterForEventHandler>();
        services.AddScoped<CancelRegistrationHandler>();
        services.AddScoped<GetMyUpcomingEventHandler>();
        services.AddScoped<IUserDataExporter, EventsUserDataExporter>();
        services.AddScoped<IUserDataEraser, EventsUserDataEraser>();

        // Jobs (P5.08) — resolved by Hangfire's ASP.NET Core job activator, same DI container.
        services.AddScoped<SendDueEventRemindersHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateEventValidator>();

        var connectionString = configuration.GetConnectionString("Default");
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(psql => psql.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
}
