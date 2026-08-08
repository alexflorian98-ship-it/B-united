using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class AddQuestionOptionValidator : AbstractValidator<AddQuestionOptionRequest>
{
    public AddQuestionOptionValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithErrorCode("errors.questionOption.valueRequired")
            .MaximumLength(200).WithErrorCode("errors.questionOption.valueTooLong");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(300).WithErrorCode("errors.questionOption.labelRequired");
    }
}
