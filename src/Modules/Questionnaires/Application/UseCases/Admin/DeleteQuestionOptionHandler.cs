using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class DeleteQuestionOptionHandler(DbContext dbContext)
{
    public async Task HandleAsync(Guid questionOptionId, CancellationToken cancellationToken)
    {
        var option = await dbContext.Set<QuestionOption>().SingleOrDefaultAsync(o => o.Id == questionOptionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified question option does not exist.");

        dbContext.Set<QuestionOption>().Remove(option);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
