using FluentValidation;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed class CreateProgramOfferValidator : AbstractValidator<CreateProgramOfferRequest>
{
    public CreateProgramOfferValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty().WithErrorCode("errors.billing.programIdRequired");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithErrorCode("errors.billing.amountMustBePositive");

        RuleFor(x => x.Currency)
            .NotEmpty().WithErrorCode("errors.billing.currencyRequired")
            .Matches("^[A-Z]{3}$").WithErrorCode("errors.billing.currencyInvalid");
    }
}
