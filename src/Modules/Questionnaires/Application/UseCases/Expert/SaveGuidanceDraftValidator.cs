using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

public sealed class SaveGuidanceDraftValidator : AbstractValidator<SaveGuidanceDraftRequest>
{
    public SaveGuidanceDraftValidator()
    {
        RuleFor(x => x.Body).NotEmpty().WithErrorCode("errors.guidance.bodyRequired");
    }
}
