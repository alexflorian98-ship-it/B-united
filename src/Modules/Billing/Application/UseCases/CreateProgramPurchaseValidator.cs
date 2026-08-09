using FluentValidation;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class CreateProgramPurchaseValidator : AbstractValidator<CreateProgramPurchaseRequest>
{
    public CreateProgramPurchaseValidator()
    {
        RuleFor(x => x.Outcome).IsInEnum().WithErrorCode("errors.billing.invalidCheckoutOutcome");
    }
}
