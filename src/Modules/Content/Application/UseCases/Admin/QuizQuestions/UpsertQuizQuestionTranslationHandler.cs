using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed class UpsertQuizQuestionTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertQuizQuestionTranslationCommand command, CancellationToken cancellationToken)
    {
        var questionExists = await dbContext.Set<QuizQuestion>().AnyAsync(q => q.Id == command.QuizQuestionId, cancellationToken);
        if (!questionExists)
        {
            throw new NotFoundAppException("The specified quiz question does not exist.");
        }

        var translation = await dbContext.Set<QuizQuestionTranslation>()
            .SingleOrDefaultAsync(t => t.QuizQuestionId == command.QuizQuestionId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            dbContext.Set<QuizQuestionTranslation>().Add(
                QuizQuestionTranslation.Create(command.QuizQuestionId, command.Language, command.Text));
        }
        else
        {
            translation.Update(command.Text);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
