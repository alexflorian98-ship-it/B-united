using BUnited.Modules.Content.Domain;
using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed class AddContentItemValidator : AbstractValidator<AddContentItemRequest>
{
    public AddContentItemValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.contentItem.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.contentItem.titleRequired");

        RuleFor(x => x.VideoReference)
            .NotEmpty().WithErrorCode("errors.contentItem.videoReferenceRequired")
            .When(x => x.Type == ContentItemType.Video);

        RuleFor(x => x.Body)
            .NotEmpty().WithErrorCode("errors.contentItem.bodyRequired")
            .When(x => x.Type == ContentItemType.RichText);
    }
}
