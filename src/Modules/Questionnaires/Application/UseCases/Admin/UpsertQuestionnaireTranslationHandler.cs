using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class UpsertQuestionnaireTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertQuestionnaireTranslationCommand command, CancellationToken cancellationToken)
    {
        var questionnaireExists = await dbContext.Set<Questionnaire>().AnyAsync(q => q.Id == command.QuestionnaireId, cancellationToken);
        if (!questionnaireExists)
        {
            throw new NotFoundAppException("The specified questionnaire does not exist.");
        }

        var translation = await dbContext.Set<QuestionnaireTranslation>()
            .SingleOrDefaultAsync(t => t.QuestionnaireId == command.QuestionnaireId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            translation = QuestionnaireTranslation.Create(command.QuestionnaireId, command.Language, command.Title, command.Description);
            dbContext.Set<QuestionnaireTranslation>().Add(translation);
        }
        else
        {
            translation.Update(command.Title, command.Description);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
