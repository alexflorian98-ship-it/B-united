using BUnited.BuildingBlocks.Observability.ErrorHandling;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.BuildingBlocks.Observability.Tests;

public sealed class FluentValidationActionFilterTests
{
    private sealed record SampleDto(string Email);

    private sealed class SampleDtoValidator : AbstractValidator<SampleDto>
    {
        public SampleDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithErrorCode("errors.email.required");
        }
    }

    private static ActionExecutingContext CreateContext(IServiceProvider serviceProvider, object argument)
    {
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["dto"] = argument },
            controller: new object());
    }

    private static ActionExecutionDelegate NextDelegate(ActionExecutingContext context, Action onCalled) =>
        () =>
        {
            onCalled();
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        };

    [Fact]
    public async Task Throws_ValidationException_when_registered_validator_fails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleDto>, SampleDtoValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateContext(serviceProvider, new SampleDto(""));
        var filter = new FluentValidationActionFilter(serviceProvider);
        var nextCalled = false;

        await Assert.ThrowsAsync<ValidationException>(
            () => filter.OnActionExecutionAsync(context, NextDelegate(context, () => nextCalled = true)));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Calls_next_when_validator_passes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleDto>, SampleDtoValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateContext(serviceProvider, new SampleDto("ada@example.com"));
        var filter = new FluentValidationActionFilter(serviceProvider);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, NextDelegate(context, () => nextCalled = true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Passes_through_when_no_validator_registered_for_argument_type()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = CreateContext(serviceProvider, new SampleDto(""));
        var filter = new FluentValidationActionFilter(serviceProvider);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, NextDelegate(context, () => nextCalled = true));

        Assert.True(nextCalled);
    }
}
