using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class UpsertQuestionTranslationValidator : AbstractValidator<UpsertQuestionTranslationRequest>
{
    public UpsertQuestionTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.question.languageRequired");
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000).WithErrorCode("errors.question.textRequired");
        RuleFor(x => x.HelpText).MaximumLength(1000).WithErrorCode("errors.question.helpTextTooLong");
    }
}
