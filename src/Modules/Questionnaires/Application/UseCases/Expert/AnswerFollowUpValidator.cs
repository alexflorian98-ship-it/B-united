using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

public sealed class AnswerFollowUpValidator : AbstractValidator<AnswerFollowUpRequest>
{
    public AnswerFollowUpValidator()
    {
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(4000).WithErrorCode("errors.guidance.followUpAnswerRequired");
    }
}
