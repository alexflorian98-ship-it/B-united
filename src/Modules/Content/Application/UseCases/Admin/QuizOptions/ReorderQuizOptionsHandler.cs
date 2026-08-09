using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed class ReorderQuizOptionsHandler(DbContext dbContext)
{
    public async Task HandleAsync(ReorderQuizOptionsCommand command, CancellationToken cancellationToken)
    {
        var options = await dbContext.Set<QuizOption>()
            .Where(o => o.QuizQuestionId == command.QuizQuestionId)
            .ToListAsync(cancellationToken);

        var existingIds = options.Select(o => o.Id).ToHashSet();
        if (existingIds.Count != command.OrderedQuizOptionIds.Count || !existingIds.SetEquals(command.OrderedQuizOptionIds))
        {
            throw new BusinessRuleAppException(
                "QUIZ_OPTION_REORDER_SET_MISMATCH",
                "errors.quizOption.reorderSetMismatch",
                "The reorder request must include exactly the question's current options, no more and no fewer.");
        }

        for (var index = 0; index < command.OrderedQuizOptionIds.Count; index++)
        {
            options.Single(o => o.Id == command.OrderedQuizOptionIds[index]).Reorder(index);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
