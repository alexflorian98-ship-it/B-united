using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class UpsertQuestionnaireTranslationValidator : AbstractValidator<UpsertQuestionnaireTranslationRequest>
{
    public UpsertQuestionnaireTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.questionnaire.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.questionnaire.titleRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.questionnaire.descriptionRequired");
    }
}
