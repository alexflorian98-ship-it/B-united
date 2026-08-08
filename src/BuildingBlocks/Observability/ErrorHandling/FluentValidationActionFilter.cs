using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BUnited.BuildingBlocks.Observability.ErrorHandling;

/// <summary>
/// Runs every action-argument object through its registered <see cref="IValidator{T}"/> (if
/// any) before the controller action executes, throwing FluentValidation's
/// <see cref="ValidationException"/> on failure so <see cref="GlobalExceptionHandler"/> can map
/// it to the standard field-error response. Arguments with no registered validator pass
/// through untouched. Register via
/// <c>AddControllers(options => options.Filters.Add&lt;FluentValidationActionFilter&gt;())</c>.
/// </summary>
public sealed class FluentValidationActionFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
