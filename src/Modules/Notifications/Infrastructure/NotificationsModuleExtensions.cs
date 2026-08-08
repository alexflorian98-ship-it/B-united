using BUnited.Modules.Notifications.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Notifications.Infrastructure;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationSender, LoggingNotificationSender>();
        return services;
    }
}
