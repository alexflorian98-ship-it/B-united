using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;

public sealed class AddQuizQuestionHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(AddQuizQuestionCommand command, CancellationToken cancellationToken)
    {
        var item = await dbContext.Set<ContentItem>().SingleOrDefaultAsync(c => c.Id == command.ContentItemId, cancellationToken)
            ?? throw new NotFoundAppException("The specified content item does not exist.");

        if (item.Type != ContentItemType.Quiz)
        {
            throw new BusinessRuleAppException("CONTENT_ITEM_NOT_A_QUIZ", "errors.contentItem.notAQuiz", "This content item is not a quiz.");
        }

        var nextSortOrder = await dbContext.Set<QuizQuestion>()
            .Where(q => q.ContentItemId == command.ContentItemId)
            .Select(q => (int?)q.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var question = QuizQuestion.Create(command.ContentItemId, nextSortOrder + 1);
        dbContext.Set<QuizQuestion>().Add(question);

        dbContext.Set<QuizQuestionTranslation>().Add(
            QuizQuestionTranslation.Create(question.Id, command.Language, command.Text));

        await dbContext.SaveChangesAsync(cancellationToken);

        return question.Id;
    }
}
