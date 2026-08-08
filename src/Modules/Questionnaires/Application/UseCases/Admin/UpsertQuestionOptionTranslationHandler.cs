using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class UpsertQuestionOptionTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertQuestionOptionTranslationCommand command, CancellationToken cancellationToken)
    {
        var optionExists = await dbContext.Set<QuestionOption>().AnyAsync(o => o.Id == command.QuestionOptionId, cancellationToken);
        if (!optionExists)
        {
            throw new NotFoundAppException("The specified question option does not exist.");
        }

        var translation = await dbContext.Set<QuestionOptionTranslation>()
            .SingleOrDefaultAsync(t => t.QuestionOptionId == command.QuestionOptionId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            translation = QuestionOptionTranslation.Create(command.QuestionOptionId, command.Language, command.Label);
            dbContext.Set<QuestionOptionTranslation>().Add(translation);
        }
        else
        {
            translation.Update(command.Label);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
