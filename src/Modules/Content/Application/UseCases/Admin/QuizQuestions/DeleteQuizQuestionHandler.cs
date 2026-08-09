using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed class DeleteQuizQuestionHandler(DbContext dbContext)
{
    public async Task HandleAsync(Guid quizQuestionId, CancellationToken cancellationToken)
    {
        var question = await dbContext.Set<QuizQuestion>().SingleOrDefaultAsync(q => q.Id == quizQuestionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified quiz question does not exist.");

        dbContext.Set<QuizQuestion>().Remove(question);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
