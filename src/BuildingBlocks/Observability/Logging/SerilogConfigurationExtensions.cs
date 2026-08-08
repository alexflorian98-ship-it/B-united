using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Formatting.Compact;

namespace BUnited.BuildingBlocks.Observability.Logging;

/// <summary>
/// Wires the shared BUnited Serilog configuration: structured console + rolling file sinks,
/// correlation-id/environment enrichment and the sensitive-field redaction hook. Individual
/// sink/level overrides can still be supplied through the "Serilog" configuration section.
/// </summary>
public static class SerilogConfigurationExtensions
{
    private const string LogFilePathTemplate = "logs/bunited-.log";

    public static WebApplicationBuilder AddBUnitedLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "BUnited.Api")
                .Destructure.With<SensitiveDataDestructuringPolicy>()
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    LogFilePathTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);
        });

        return builder;
    }
}
