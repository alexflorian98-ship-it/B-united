using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class UpsertQuestionOptionTranslationValidator : AbstractValidator<UpsertQuestionOptionTranslationRequest>
{
    public UpsertQuestionOptionTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.questionOption.languageRequired");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(300).WithErrorCode("errors.questionOption.labelRequired");
    }
}
