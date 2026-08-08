using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.Revoke;

public sealed class RevokeTokenValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithErrorCode("errors.token.required");
    }
}
