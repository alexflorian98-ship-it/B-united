using BUnited.BuildingBlocks.Application.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BUnited.BuildingBlocks.Infrastructure.Safety;

/// <summary>P3.32: the application MUST refuse to start in <c>Production</c> if any demo-only
/// adapter (<see cref="IDemoOnlyAdapter"/> — <c>FakePaymentProvider</c>,
/// <c>LoggingNotificationSender</c>, <c>LoggingIdentityEmailSender</c>) is registered. Call this
/// once, after every module's <c>AddXModule()</c> registration, so the full DI graph is
/// scanned — not before, or a later registration could slip past unchecked.</summary>
public static class ProductionSafetyExtensions
{
    public static IServiceCollection VerifyNoDemoOnlyAdaptersInProduction(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return services;
        }

        var demoAdapters = services
            .Where(d => d.ImplementationType is not null && typeof(IDemoOnlyAdapter).IsAssignableFrom(d.ImplementationType))
            .Select(d => d.ImplementationType!.FullName)
            .Distinct()
            .ToList();

        if (demoAdapters.Count > 0)
        {
            throw new InvalidOperationException(
                "Refusing to start in Production: the following demo-only adapters are registered " +
                $"(see ADR-010, P3.32): {string.Join(", ", demoAdapters)}. Replace them with real " +
                "provider implementations before deploying to Production.");
        }

        return services;
    }
}
