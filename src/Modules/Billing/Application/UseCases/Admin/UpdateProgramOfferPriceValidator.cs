using FluentValidation;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed class UpdateProgramOfferPriceValidator : AbstractValidator<UpdateProgramOfferPriceRequest>
{
    public UpdateProgramOfferPriceValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithErrorCode("errors.billing.amountMustBePositive");

        RuleFor(x => x.Currency)
            .NotEmpty().WithErrorCode("errors.billing.currencyRequired")
            .Matches("^[A-Z]{3}$").WithErrorCode("errors.billing.currencyInvalid");
    }
}
