using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed class UpsertContentItemTranslationValidator : AbstractValidator<UpsertContentItemTranslationRequest>
{
    public UpsertContentItemTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.contentItem.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.contentItem.titleRequired");
    }
}
