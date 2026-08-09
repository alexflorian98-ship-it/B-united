using FluentValidation;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class UpsertEventTranslationValidator : AbstractValidator<UpsertEventTranslationRequest>
{
    public UpsertEventTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.event.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.event.titleRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.event.descriptionRequired");
    }
}
