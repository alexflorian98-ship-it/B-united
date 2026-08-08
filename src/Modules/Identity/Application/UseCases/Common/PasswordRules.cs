using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.Common;

/// <summary>Shared password-strength rules for registration and password reset.</summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordStrengthRules<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithErrorCode("errors.password.required")
            .MinimumLength(10).WithErrorCode("errors.password.tooShort")
            .Matches("[A-Z]").WithErrorCode("errors.password.requiresUppercase")
            .Matches("[a-z]").WithErrorCode("errors.password.requiresLowercase")
            .Matches("[0-9]").WithErrorCode("errors.password.requiresDigit");
}
