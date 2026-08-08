using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.VerifyEmail;

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithErrorCode("errors.token.required");
    }
}
