using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed record PublishedQuestionnaireSummaryDto(Guid Id, string Title, string Description);

/// <summary>Lets a client discover which questionnaire(s) they can start — not covered by
/// docs/PROMPT.md §25–28's flow description directly (it assumes a known questionnaire), but
/// required for the client UI to have anything to link to; kept minimal (id + title +
/// description only, no question content) since starting still goes through the consent gate.
/// Deliberately left unfiltered by <c>IProgramAccessContext</c> (ADR-003): this is browsable
/// catalogue metadata, not the protected resource, mirroring Content's published-programs
/// catalogue staying open while only the detail (here: <see cref="GetClientQuestionnaireHandler"/>'s
/// full question content) and every downstream submission/guidance action are paywalled.</summary>
public sealed class ListPublishedQuestionnairesHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<PublishedQuestionnaireSummaryDto>> HandleAsync(string requestedLanguage, CancellationToken cancellationToken)
    {
        var questionnaires = await dbContext.Set<Questionnaire>()
            .Where(q => q.Status == QuestionnaireStatus.Published)
            .ToListAsync(cancellationToken);
        var ids = questionnaires.Select(q => q.Id).ToList();

        var translations = await dbContext.Set<QuestionnaireTranslation>()
            .Where(t => ids.Contains(t.QuestionnaireId))
            .ToListAsync(cancellationToken);

        return questionnaires.Select(q =>
        {
            var resolution = TranslationResolver.Resolve(translations.Where(t => t.QuestionnaireId == q.Id), requestedLanguage, q.DefaultLanguage);
            return new PublishedQuestionnaireSummaryDto(q.Id, resolution.Translation.Title, resolution.Translation.Description);
        }).ToList();
    }
}
