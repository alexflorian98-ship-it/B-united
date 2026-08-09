using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Progress.Application.UseCases;
using BUnited.Modules.Progress.Application.UseCases.DataRights;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Progress.Infrastructure;

public static class ProgressModuleExtensions
{
    public static IServiceCollection AddProgressModule(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<RecordVideoProgressHandler>();
        services.AddScoped<MarkContentCompletedHandler>();
        services.AddScoped<GetContentProgressHandler>();
        services.AddScoped<GetSectionProgressHandler>();
        services.AddScoped<IUserDataExporter, ProgressUserDataExporter>();
        services.AddScoped<IUserDataEraser, ProgressUserDataEraser>();

        services.AddValidatorsFromAssemblyContaining<RecordVideoProgressValidator>();

        return services;
    }
}
