using FluentValidation;

namespace BUnited.Modules.Identity.Application.UseCases.Profile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    /// <summary>Kept in sync with the frontend's <c>SUPPORTED_LANGUAGES</c> (frontend/src/shared/i18n/i18n.ts).</summary>
    private static readonly string[] SupportedLanguages = ["ro", "en"];

    public UpdateProfileValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty().WithErrorCode("errors.timezone.required")
            .Must(BeAKnownTimeZone).WithErrorCode("errors.timezone.invalid");

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty().WithErrorCode("errors.language.required")
            .Must(language => SupportedLanguages.Contains(language)).WithErrorCode("errors.language.unsupported");
    }

    private static bool BeAKnownTimeZone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
    }
}
