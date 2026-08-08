using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Audit.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Audit.Infrastructure;

public static class AuditModuleExtensions
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuditLogger, AuditLogger>();

        return services;
    }
}
