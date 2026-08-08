using FluentValidation;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class SaveDraftAnswersValidator : AbstractValidator<SaveDraftAnswersRequest>
{
    public SaveDraftAnswersValidator()
    {
        RuleFor(x => x.Answers).NotEmpty().WithErrorCode("errors.questionnaire.answersRequired");
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId).NotEmpty().WithErrorCode("errors.questionnaire.answerQuestionRequired");
            answer.RuleFor(a => a.Value).NotNull().WithErrorCode("errors.questionnaire.answerValueRequired");
        });
    }
}
