using FluentValidation;

namespace BUnited.Modules.Progress.Application.UseCases;

public sealed class RecordVideoProgressValidator : AbstractValidator<RecordVideoProgressRequest>
{
    public RecordVideoProgressValidator()
    {
        RuleFor(x => x.ContentItemId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.SectionContentItemIds).NotEmpty().WithErrorCode("errors.progress.sectionItemsRequired");
        RuleFor(x => x.PositionSeconds).GreaterThanOrEqualTo(0).WithErrorCode("errors.progress.positionInvalid");
        RuleFor(x => x.WatchPercentage).InclusiveBetween(0, 100).WithErrorCode("errors.progress.percentageInvalid");
    }
}
