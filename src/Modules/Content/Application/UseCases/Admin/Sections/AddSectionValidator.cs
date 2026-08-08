using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Sections;

public sealed class AddSectionValidator : AbstractValidator<AddSectionRequest>
{
    public AddSectionValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.section.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.section.titleRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.section.descriptionRequired");
    }
}
