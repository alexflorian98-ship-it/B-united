using BUnited.Modules.Notifications.Contracts;
using BUnited.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Notifications.Tests;

/// <summary>
/// Guards the module's DI wiring: every other module resolves <see cref="INotificationSender"/>
/// only through this registration (never `new LoggingNotificationSender(...)` directly), so a
/// silent change here (e.g. someone swapping in a different implementation, or forgetting to
/// register it as scoped) would break every caller without any compiler error.
/// </summary>
public sealed class NotificationsModuleExtensionsTests
{
    [Fact]
    public void AddNotificationsModule_registers_INotificationSender_as_LoggingNotificationSender()
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddLogging();
        services.AddNotificationsModule();

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<INotificationSender>();

        Assert.IsType<LoggingNotificationSender>(sender);
    }

    [Fact]
    public void AddNotificationsModule_registers_INotificationSender_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationsModule();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(INotificationSender));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
