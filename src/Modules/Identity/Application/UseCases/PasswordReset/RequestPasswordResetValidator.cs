using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.PasswordReset;

public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetValidator()
    {
        // Deliberately no "email exists" check here — the handler always returns success
        // regardless of whether the account exists (§22.a), so the validator must not leak
        // account existence through a field-level error either.
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("errors.email.required")
            .EmailAddress().WithErrorCode("errors.email.invalid");
    }
}
