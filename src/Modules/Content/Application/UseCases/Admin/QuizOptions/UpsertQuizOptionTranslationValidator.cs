using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed class UpsertQuizOptionTranslationValidator : AbstractValidator<UpsertQuizOptionTranslationRequest>
{
    public UpsertQuizOptionTranslationValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.quizOption.languageRequired");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(300).WithErrorCode("errors.quizOption.labelRequired");
    }
}
