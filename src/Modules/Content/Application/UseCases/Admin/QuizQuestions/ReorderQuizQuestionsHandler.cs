using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed class ReorderQuizQuestionsHandler(DbContext dbContext)
{
    public async Task HandleAsync(ReorderQuizQuestionsCommand command, CancellationToken cancellationToken)
    {
        var questions = await dbContext.Set<QuizQuestion>()
            .Where(q => q.ContentItemId == command.ContentItemId)
            .ToListAsync(cancellationToken);

        var existingIds = questions.Select(q => q.Id).ToHashSet();
        if (existingIds.Count != command.OrderedQuizQuestionIds.Count || !existingIds.SetEquals(command.OrderedQuizQuestionIds))
        {
            throw new BusinessRuleAppException(
                "QUIZ_QUESTION_REORDER_SET_MISMATCH",
                "errors.quizQuestion.reorderSetMismatch",
                "The reorder request must include exactly the quiz's current questions, no more and no fewer.");
        }

        for (var index = 0; index < command.OrderedQuizQuestionIds.Count; index++)
        {
            questions.Single(q => q.Id == command.OrderedQuizQuestionIds[index]).Reorder(index);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
