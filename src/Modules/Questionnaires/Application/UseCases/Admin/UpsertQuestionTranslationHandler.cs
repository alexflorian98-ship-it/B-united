using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class UpsertQuestionTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertQuestionTranslationCommand command, CancellationToken cancellationToken)
    {
        var questionExists = await dbContext.Set<Question>().AnyAsync(q => q.Id == command.QuestionId, cancellationToken);
        if (!questionExists)
        {
            throw new NotFoundAppException("The specified question does not exist.");
        }

        var translation = await dbContext.Set<QuestionTranslation>()
            .SingleOrDefaultAsync(t => t.QuestionId == command.QuestionId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            translation = QuestionTranslation.Create(command.QuestionId, command.Language, command.Text, command.HelpText);
            dbContext.Set<QuestionTranslation>().Add(translation);
        }
        else
        {
            translation.Update(command.Text, command.HelpText);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
