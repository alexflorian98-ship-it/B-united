using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class CreateProgramValidator : AbstractValidator<CreateProgramRequest>
{
    public CreateProgramValidator()
    {
        RuleFor(x => x.DomainId).NotEmpty().WithErrorCode("errors.program.domainRequired");

        RuleFor(x => x.Slug)
            .NotEmpty().WithErrorCode("errors.program.slugRequired")
            .MaximumLength(200).WithErrorCode("errors.program.slugTooLong")
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithErrorCode("errors.program.slugInvalid");

        RuleFor(x => x.DefaultLanguage).NotEmpty().WithErrorCode("errors.program.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.program.titleRequired");
        RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500).WithErrorCode("errors.program.shortDescriptionRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.program.descriptionRequired");
    }
}
