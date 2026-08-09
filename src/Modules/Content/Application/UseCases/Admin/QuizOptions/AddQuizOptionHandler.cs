using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

/// <summary>V1 quiz questions are single-correct-answer/single-select (Phase 1 plan) — enforced
/// here at add-time by rejecting a second <c>IsCorrect = true</c> option for the same question,
/// rather than deferring the check to publish-time. Add-time rejection gives the admin immediate
/// feedback exactly at the point of the mistake and keeps the invariant true at every moment in
/// the question's lifecycle, not just at publish.</summary>
public sealed class AddQuizOptionHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(AddQuizOptionCommand command, CancellationToken cancellationToken)
    {
        var questionExists = await dbContext.Set<QuizQuestion>().AnyAsync(q => q.Id == command.QuizQuestionId, cancellationToken);
        if (!questionExists)
        {
            throw new NotFoundAppException("The specified quiz question does not exist.");
        }

        if (command.IsCorrect)
        {
            var alreadyHasCorrectAnswer = await dbContext.Set<QuizOption>()
                .AnyAsync(o => o.QuizQuestionId == command.QuizQuestionId && o.IsCorrect, cancellationToken);
            if (alreadyHasCorrectAnswer)
            {
                throw new BusinessRuleAppException(
                    "QUIZ_OPTION_ALREADY_HAS_CORRECT_ANSWER",
                    "errors.quizOption.correctAnswerAlreadySet",
                    "This question already has a correct option; V1 quiz questions allow exactly one.");
            }
        }

        var nextSortOrder = await dbContext.Set<QuizOption>()
            .Where(o => o.QuizQuestionId == command.QuizQuestionId)
            .Select(o => (int?)o.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var option = QuizOption.Create(command.QuizQuestionId, command.IsCorrect, nextSortOrder + 1);
        dbContext.Set<QuizOption>().Add(option);

        dbContext.Set<QuizOptionTranslation>().Add(
            QuizOptionTranslation.Create(option.Id, command.Language, command.Label));

        await dbContext.SaveChangesAsync(cancellationToken);

        return option.Id;
    }
}
