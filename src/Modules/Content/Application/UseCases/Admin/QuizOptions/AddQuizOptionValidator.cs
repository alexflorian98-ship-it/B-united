using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed class AddQuizOptionValidator : AbstractValidator<AddQuizOptionRequest>
{
    public AddQuizOptionValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.quizOption.languageRequired");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(300).WithErrorCode("errors.quizOption.labelRequired");
    }
}
