using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Configuration;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Application.UseCases.Admin;
using BUnited.Modules.Billing.Infrastructure.Access;
using BUnited.Modules.Billing.Infrastructure.Payments;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Billing.Infrastructure;

public static class BillingModuleExtensions
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BillingOptions>(configuration.GetSection(BillingOptions.SectionName));

        services.AddScoped<IPaymentProvider, FakePaymentProvider>();
        services.AddScoped<IAccessContext, BillingAccessContext>();

        services.AddScoped<ProcessProviderEventHandler>();
        services.AddScoped<CreateCheckoutHandler>();
        services.AddScoped<TriggerDemoEventHandler>();
        services.AddScoped<GetMyBillingStatusHandler>();
        services.AddScoped<ListPlansHandler>();
        services.AddScoped<ListSubscribersHandler>();
        services.AddScoped<GetSubscriptionDetailHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateCheckoutValidator>();

        return services;
    }
}
