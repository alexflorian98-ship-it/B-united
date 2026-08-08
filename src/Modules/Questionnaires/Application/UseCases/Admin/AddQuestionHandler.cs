using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class AddQuestionHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(AddQuestionCommand command, CancellationToken cancellationToken)
    {
        var questionnaire = await dbContext.Set<Questionnaire>().SingleOrDefaultAsync(q => q.Id == command.QuestionnaireId, cancellationToken)
            ?? throw new NotFoundAppException("The specified questionnaire does not exist.");

        var type = Enum.Parse<QuestionType>(command.Type);

        var nextSortOrder = await dbContext.Set<Question>()
            .Where(q => q.QuestionnaireId == command.QuestionnaireId)
            .CountAsync(cancellationToken);

        var question = Question.Create(command.QuestionnaireId, type, nextSortOrder, command.IsRequired, command.ActorId);
        dbContext.Set<Question>().Add(question);

        var translation = QuestionTranslation.Create(question.Id, questionnaire.DefaultLanguage, command.Text, command.HelpText);
        dbContext.Set<QuestionTranslation>().Add(translation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return question.Id;
    }
}
