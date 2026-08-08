using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class AddQuestionOptionHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(AddQuestionOptionCommand command, CancellationToken cancellationToken)
    {
        var question = await dbContext.Set<Question>().SingleOrDefaultAsync(q => q.Id == command.QuestionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified question does not exist.");

        if (question.Type is not (QuestionType.SingleChoice or QuestionType.MultiChoice or QuestionType.Scale))
        {
            throw new BusinessRuleAppException(
                "QUESTION_OPTION_TYPE_INVALID",
                "errors.questionOption.typeInvalid",
                "Options can only be added to SingleChoice, MultiChoice or Scale questions.");
        }

        var valueTaken = await dbContext.Set<QuestionOption>()
            .AnyAsync(o => o.QuestionId == command.QuestionId && o.Value == command.Value, cancellationToken);
        if (valueTaken)
        {
            throw new BusinessRuleAppException("QUESTION_OPTION_VALUE_TAKEN", "errors.questionOption.valueTaken", "This option value already exists on this question.");
        }

        var nextSortOrder = await dbContext.Set<QuestionOption>()
            .Where(o => o.QuestionId == command.QuestionId)
            .CountAsync(cancellationToken);

        var questionnaire = await dbContext.Set<Questionnaire>()
            .SingleAsync(q => q.Id == question.QuestionnaireId, cancellationToken);

        var option = QuestionOption.Create(command.QuestionId, command.Value, nextSortOrder);
        dbContext.Set<QuestionOption>().Add(option);

        var translation = QuestionOptionTranslation.Create(option.Id, questionnaire.DefaultLanguage, command.Label);
        dbContext.Set<QuestionOptionTranslation>().Add(translation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return option.Id;
    }
}
