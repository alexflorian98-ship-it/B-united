using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;

public sealed class UpsertQuizOptionTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertQuizOptionTranslationCommand command, CancellationToken cancellationToken)
    {
        var optionExists = await dbContext.Set<QuizOption>().AnyAsync(o => o.Id == command.QuizOptionId, cancellationToken);
        if (!optionExists)
        {
            throw new NotFoundAppException("The specified quiz option does not exist.");
        }

        var translation = await dbContext.Set<QuizOptionTranslation>()
            .SingleOrDefaultAsync(t => t.QuizOptionId == command.QuizOptionId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            dbContext.Set<QuizOptionTranslation>().Add(
                QuizOptionTranslation.Create(command.QuizOptionId, command.Language, command.Label));
        }
        else
        {
            translation.Update(command.Label);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
