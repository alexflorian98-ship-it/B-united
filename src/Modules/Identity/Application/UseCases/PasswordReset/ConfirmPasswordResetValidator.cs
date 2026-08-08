using BUnited.Modules.Identity.Application.UseCases.Common;
using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.PasswordReset;

public sealed class ConfirmPasswordResetValidator : AbstractValidator<ConfirmPasswordResetCommand>
{
    public ConfirmPasswordResetValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithErrorCode("errors.token.required");
        RuleFor(x => x.NewPassword).ApplyPasswordStrengthRules();
    }
}
