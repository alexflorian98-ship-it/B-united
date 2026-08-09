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

        // A quiz starts empty: neither a video reference nor a body applies. Its questions are
        // added via separate calls after creation, matching how translations are already a
        // separate upsert step from creation for every content-item type.
        RuleFor(x => x.VideoReference)
            .Empty().WithErrorCode("errors.contentItem.videoReferenceNotAllowed")
            .When(x => x.Type == ContentItemType.Quiz);

        RuleFor(x => x.Body)
            .Empty().WithErrorCode("errors.contentItem.bodyNotAllowed")
            .When(x => x.Type == ContentItemType.Quiz);
    }
}
