using Microsoft.Extensions.DependencyInjection;

namespace BUnited.BuildingBlocks.Observability.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddBUnitedErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
