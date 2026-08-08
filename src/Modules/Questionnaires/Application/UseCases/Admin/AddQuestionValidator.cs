using BUnited.Modules.Questionnaires.Domain;
using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class AddQuestionValidator : AbstractValidator<AddQuestionRequest>
{
    public AddQuestionValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithErrorCode("errors.question.typeRequired")
            .Must(type => Enum.TryParse<QuestionType>(type, out _)).WithErrorCode("errors.question.typeInvalid");

        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000).WithErrorCode("errors.question.textRequired");
        RuleFor(x => x.HelpText).MaximumLength(1000).WithErrorCode("errors.question.helpTextTooLong");
    }
}
