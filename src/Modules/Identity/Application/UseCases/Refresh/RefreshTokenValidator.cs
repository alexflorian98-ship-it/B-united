using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.Refresh;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithErrorCode("errors.token.required");
    }
}
