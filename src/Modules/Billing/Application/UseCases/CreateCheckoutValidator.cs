using FluentValidation;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class CreateCheckoutValidator : AbstractValidator<CreateCheckoutRequest>
{
    public CreateCheckoutValidator()
    {
        RuleFor(x => x.PlanPriceId).NotEmpty().WithErrorCode("errors.billing.planPriceRequired");
    }
}
