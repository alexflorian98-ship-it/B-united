using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class CreateQuestionnaireValidator : AbstractValidator<CreateQuestionnaireRequest>
{
    public CreateQuestionnaireValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty().WithErrorCode("errors.questionnaire.programRequired");
        RuleFor(x => x.DefaultLanguage).NotEmpty().WithErrorCode("errors.questionnaire.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.questionnaire.titleRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.questionnaire.descriptionRequired");
    }
}
