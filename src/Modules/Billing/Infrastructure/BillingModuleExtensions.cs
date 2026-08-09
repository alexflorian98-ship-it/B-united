using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Configuration;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Application.UseCases.Admin;
using BUnited.Modules.Billing.Application.UseCases.DataRights;
using BUnited.Modules.Billing.Contracts;
using BUnited.Modules.Billing.Infrastructure.Access;
using BUnited.Modules.Billing.Infrastructure.CrossModule;
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
        services.AddScoped<IProgramAccessContext, BillingProgramAccessContext>();
        services.AddScoped<IProgramOfferLookup, ProgramOfferLookup>();

        services.AddScoped<ProcessProviderEventHandler>();
        services.AddScoped<CreateProgramPurchaseHandler>();
        services.AddScoped<TriggerDemoEventHandler>();
        services.AddScoped<ListMyPurchasesHandler>();
        services.AddScoped<ListMyInvoicesHandler>();
        services.AddScoped<ListMyEntitlementsHandler>();
        services.AddScoped<ListPurchasesHandler>();
        services.AddScoped<GetPurchaseDetailHandler>();
        services.AddScoped<IUserDataExporter, BillingUserDataExporter>();

        services.AddScoped<CreateProgramOfferHandler>();
        services.AddScoped<UpdateProgramOfferPriceHandler>();
        services.AddScoped<ProgramOfferStatusHandler>();
        services.AddScoped<ListProgramOffersHandler>();
        services.AddScoped<GetProgramOfferDetailHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateProgramPurchaseValidator>();

        return services;
    }
}
