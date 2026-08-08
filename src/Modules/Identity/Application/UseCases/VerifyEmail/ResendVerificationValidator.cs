using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.VerifyEmail;

public sealed class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("errors.email.required")
            .EmailAddress().WithErrorCode("errors.email.invalid");
    }
}
