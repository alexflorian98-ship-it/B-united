using FluentValidation;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed class AddQuizQuestionValidator : AbstractValidator<AddQuizQuestionRequest>
{
    public AddQuizQuestionValidator()
    {
        RuleFor(x => x.Language).NotEmpty().WithErrorCode("errors.quizQuestion.languageRequired");
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000).WithErrorCode("errors.quizQuestion.textRequired");
    }
}
