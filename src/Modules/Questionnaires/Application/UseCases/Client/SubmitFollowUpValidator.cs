using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class SubmitFollowUpValidator : AbstractValidator<SubmitFollowUpRequest>
{
    public SubmitFollowUpValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000).WithErrorCode("errors.guidance.followUpQuestionRequired");
    }
}
