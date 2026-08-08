using FluentValidation;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed class MarkContentCompletedValidator : AbstractValidator<MarkContentCompletedRequest>
{
    public MarkContentCompletedValidator()
    {
        RuleFor(x => x.ContentItemId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.SectionContentItemIds).NotEmpty().WithErrorCode("errors.progress.sectionItemsRequired");
    }
}
