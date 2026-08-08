using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class ListQuestionnairesHandler(DbContext dbContext)
{
    public async Task<QuestionnaireListResult> HandleAsync(ListQuestionnairesQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<Questionnaire>().AsQueryable();
        if (query.Status is not null)
        {
            baseQuery = baseQuery.Where(q => q.Status == query.Status);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .OrderByDescending(q => q.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var questionnaireIds = page.Select(q => q.Id).ToList();

        var translations = await dbContext.Set<QuestionnaireTranslation>()
            .Where(t => questionnaireIds.Contains(t.QuestionnaireId))
            .ToListAsync(cancellationToken);

        var questionCounts = await dbContext.Set<Question>()
            .Where(q => questionnaireIds.Contains(q.QuestionnaireId))
            .GroupBy(q => q.QuestionnaireId)
            .Select(g => new { QuestionnaireId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var items = page.Select(questionnaire =>
        {
            var questionnaireTranslations = translations.Where(t => t.QuestionnaireId == questionnaire.Id).ToList();
            var title = questionnaireTranslations.FirstOrDefault(t => t.Language == questionnaire.DefaultLanguage)?.Title
                ?? questionnaireTranslations.FirstOrDefault()?.Title
                ?? questionnaire.Id.ToString();

            return new QuestionnaireSummaryDto(
                questionnaire.Id,
                questionnaire.Status.ToString(),
                questionnaire.DefaultLanguage,
                title,
                questionCounts.FirstOrDefault(c => c.QuestionnaireId == questionnaire.Id)?.Count ?? 0,
                questionnaireTranslations.Select(t => t.Language).OrderBy(l => l).ToList(),
                questionnaire.UpdatedAt);
        }).ToList();

        return new QuestionnaireListResult(items, totalCount);
    }
}
