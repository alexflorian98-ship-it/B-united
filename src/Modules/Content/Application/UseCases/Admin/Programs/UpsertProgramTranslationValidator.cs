using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class UpsertProgramTranslationValidator : AbstractValidator<UpsertProgramTranslationRequest>
{
    public UpsertProgramTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.program.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.program.titleRequired");
        RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500).WithErrorCode("errors.program.shortDescriptionRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.program.descriptionRequired");
    }
}
