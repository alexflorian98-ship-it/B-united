using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class DeleteQuestionHandler(DbContext dbContext)
{
    public async Task HandleAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await dbContext.Set<Question>().SingleOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified question does not exist.");

        dbContext.Set<Question>().Remove(question);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
