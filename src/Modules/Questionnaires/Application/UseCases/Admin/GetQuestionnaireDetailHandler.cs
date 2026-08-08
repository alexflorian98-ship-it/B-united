using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class GetQuestionnaireDetailHandler(DbContext dbContext)
{
    public async Task<QuestionnaireDetailDto> HandleAsync(Guid questionnaireId, CancellationToken cancellationToken)
    {
        var questionnaire = await dbContext.Set<Questionnaire>().SingleOrDefaultAsync(q => q.Id == questionnaireId, cancellationToken)
            ?? throw new NotFoundAppException("The specified questionnaire does not exist.");

        var translations = await dbContext.Set<QuestionnaireTranslation>()
            .Where(t => t.QuestionnaireId == questionnaireId)
            .Select(t => new QuestionnaireTranslationDto(t.Language, t.Title, t.Description))
            .ToListAsync(cancellationToken);

        var questions = await dbContext.Set<Question>()
            .Where(q => q.QuestionnaireId == questionnaireId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(cancellationToken);
        var questionIds = questions.Select(q => q.Id).ToList();

        var questionTranslations = await dbContext.Set<QuestionTranslation>()
            .Where(t => questionIds.Contains(t.QuestionId))
            .ToListAsync(cancellationToken);

        var options = await dbContext.Set<QuestionOption>()
            .Where(o => questionIds.Contains(o.QuestionId))
            .OrderBy(o => o.SortOrder)
            .ToListAsync(cancellationToken);
        var optionIds = options.Select(o => o.Id).ToList();

        var optionTranslations = await dbContext.Set<QuestionOptionTranslation>()
            .Where(t => optionIds.Contains(t.QuestionOptionId))
            .ToListAsync(cancellationToken);

        var questionDtos = questions.Select(question => new QuestionDetailDto(
            question.Id,
            question.Type.ToString(),
            question.SortOrder,
            question.IsRequired,
            questionTranslations.Where(t => t.QuestionId == question.Id)
                .Select(t => new QuestionTranslationDto(t.Language, t.Text, t.HelpText))
                .ToList(),
            options.Where(o => o.QuestionId == question.Id)
                .Select(option => new QuestionOptionDetailDto(
                    option.Id,
                    option.Value,
                    option.SortOrder,
                    optionTranslations.Where(t => t.QuestionOptionId == option.Id)
                        .Select(t => new QuestionOptionTranslationDto(t.Language, t.Label))
                        .ToList()))
                .ToList()))
            .ToList();

        return new QuestionnaireDetailDto(
            questionnaire.Id,
            questionnaire.Status.ToString(),
            questionnaire.DefaultLanguage,
            translations,
            questionDtos);
    }
}
