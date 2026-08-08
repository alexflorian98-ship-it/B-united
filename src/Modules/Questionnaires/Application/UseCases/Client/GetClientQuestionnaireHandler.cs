using BUnited.BuildingBlocks.Application.Errors;
using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class GetClientQuestionnaireHandler(DbContext dbContext)
{
    public async Task<ClientQuestionnaireDto> HandleAsync(Guid questionnaireId, string requestedLanguage, CancellationToken cancellationToken)
    {
        var questionnaire = await dbContext.Set<Questionnaire>()
            .SingleOrDefaultAsync(q => q.Id == questionnaireId && q.Status == QuestionnaireStatus.Published, cancellationToken)
            ?? throw new NotFoundAppException("The specified questionnaire does not exist or is not published.");

        var translations = await dbContext.Set<QuestionnaireTranslation>()
            .Where(t => t.QuestionnaireId == questionnaireId)
            .ToListAsync(cancellationToken);
        var resolution = TranslationResolver.Resolve(translations, requestedLanguage, questionnaire.DefaultLanguage);

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

        var questionDtos = questions.Select(question =>
        {
            var questionResolution = TranslationResolver.Resolve(
                questionTranslations.Where(t => t.QuestionId == question.Id),
                requestedLanguage,
                questionnaire.DefaultLanguage);

            var questionOptions = options.Where(o => o.QuestionId == question.Id)
                .Select(option =>
                {
                    var optionResolution = TranslationResolver.Resolve(
                        optionTranslations.Where(t => t.QuestionOptionId == option.Id),
                        requestedLanguage,
                        questionnaire.DefaultLanguage);
                    return new ClientQuestionOptionDto(option.Value, optionResolution.Translation.Label);
                })
                .ToList();

            return new ClientQuestionDto(
                question.Id,
                question.Type.ToString(),
                question.IsRequired,
                question.SortOrder,
                questionResolution.Translation.Text,
                questionResolution.Translation.HelpText,
                questionOptions);
        }).ToList();

        return new ClientQuestionnaireDto(questionnaire.Id, resolution.Translation.Title, resolution.Translation.Description, questionDtos);
    }
}
