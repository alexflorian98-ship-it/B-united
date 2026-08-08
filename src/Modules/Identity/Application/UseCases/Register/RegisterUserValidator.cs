using BUnited.Modules.Identity.Application.UseCases.Common;
using BUnited.Modules.Identity.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Register;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator(DbContext dbContext)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("errors.email.required")
            .EmailAddress().WithErrorCode("errors.email.invalid")
            .MustAsync(async (email, cancellationToken) =>
            {
                var normalized = User.Normalize(email);
                return !await dbContext.Set<User>().AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
            }).WithErrorCode("errors.email.alreadyRegistered");

        RuleFor(x => x.Password).ApplyPasswordStrengthRules();
    }
}
