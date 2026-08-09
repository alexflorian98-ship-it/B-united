using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

/// <summary>Full question content is the paywalled resource here (the equivalent of Content's
/// program-detail body being stripped for non-owned programs) — gated on
/// <see cref="IProgramAccessContext"/> in addition to the questionnaire being published. The
/// browsable <see cref="ListPublishedQuestionnairesHandler"/> catalogue stays open by design
/// (see its own remarks) so a client can discover what a program's questionnaire covers before
/// buying, matching Content's catalogue-vs-detail split.</summary>
public sealed class GetClientQuestionnaireHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<ClientQuestionnaireDto> HandleAsync(Guid userId, Guid questionnaireId, string requestedLanguage, CancellationToken cancellationToken)
    {
        var questionnaire = await dbContext.Set<Questionnaire>()
            .SingleOrDefaultAsync(q => q.Id == questionnaireId && q.Status == QuestionnaireStatus.Published, cancellationToken)
            ?? throw new NotFoundAppException("The specified questionnaire does not exist or is not published.");

        await programAccessContext.RequireProgramAccessAsync(userId, questionnaire.ProgramId, cancellationToken);

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
