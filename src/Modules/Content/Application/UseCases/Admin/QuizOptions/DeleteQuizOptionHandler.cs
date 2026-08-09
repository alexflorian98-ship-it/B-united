using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed class DeleteQuizOptionHandler(DbContext dbContext)
{
    public async Task HandleAsync(Guid quizOptionId, CancellationToken cancellationToken)
    {
        var option = await dbContext.Set<QuizOption>().SingleOrDefaultAsync(o => o.Id == quizOptionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified quiz option does not exist.");

        dbContext.Set<QuizOption>().Remove(option);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
