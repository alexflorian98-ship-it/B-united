using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithErrorCode("errors.email.required");
        RuleFor(x => x.Password).NotEmpty().WithErrorCode("errors.password.required");
    }
}
