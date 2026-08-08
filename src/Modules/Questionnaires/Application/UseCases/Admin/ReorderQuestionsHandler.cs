using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class ReorderQuestionsHandler(DbContext dbContext)
{
    public async Task HandleAsync(ReorderQuestionsCommand command, Guid actorId, CancellationToken cancellationToken)
    {
        var questions = await dbContext.Set<Question>()
            .Where(q => q.QuestionnaireId == command.QuestionnaireId)
            .ToListAsync(cancellationToken);

        var existingIds = questions.Select(q => q.Id).ToHashSet();
        if (existingIds.Count != command.OrderedQuestionIds.Count || !existingIds.SetEquals(command.OrderedQuestionIds))
        {
            throw new BusinessRuleAppException(
                "QUESTION_REORDER_SET_MISMATCH",
                "errors.question.reorderSetMismatch",
                "The reorder request must include exactly the questionnaire's current questions, no more and no fewer.");
        }

        for (var index = 0; index < command.OrderedQuestionIds.Count; index++)
        {
            questions.Single(q => q.Id == command.OrderedQuestionIds[index]).Reorder(index, actorId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
