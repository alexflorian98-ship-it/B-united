using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.BuildingBlocks.Localization;
using BUnited.Modules.Billing.Contracts;
using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Client;

/// <summary>P3.37.a — the actual paywall for full program content: the section/content-item tree
/// only ever includes <c>Body</c>/media (<see cref="ClientContentItemDto.Body"/>/
/// <see cref="ClientContentItemDto.MediaAssetId"/>) when the resolved
/// <see cref="ClientProgramOwnershipState.Owned"/> state is confirmed via
/// <see cref="IProgramAccessContext"/>. A caller with no resolvable identity at all
/// (<paramref name="userId"/> is <see langword="null"/>) can never own anything, so it always
/// gets the same stripped view as a signed-in, non-owning caller — titles/structure/
/// <c>IsRequired</c>/item type only.</summary>
public sealed class GetPublishedProgramDetailHandler(DbContext dbContext, IProgramOfferLookup programOfferLookup, IProgramAccessContext programAccessContext)
{
    public async Task<ClientProgramDetailDto> HandleAsync(string slug, string requestedLanguage, Guid? userId, CancellationToken cancellationToken)
    {
        var program = await dbContext.Set<Program>()
            .SingleOrDefaultAsync(p => p.Slug == slug && p.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist or is not published.");

        var programTranslations = await dbContext.Set<ProgramTranslation>()
            .Where(t => t.ProgramId == program.Id)
            .ToListAsync(cancellationToken);
        var programResolution = TranslationResolver.Resolve(programTranslations, requestedLanguage, program.DefaultLanguage);

        var sections = await dbContext.Set<Section>()
            .Where(s => s.ProgramId == program.Id && s.Status == ContentStatus.Published)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);
        var sectionIds = sections.Select(s => s.Id).ToList();

        var sectionTranslations = await dbContext.Set<SectionTranslation>()
            .Where(t => sectionIds.Contains(t.SectionId))
            .ToListAsync(cancellationToken);

        var items = await dbContext.Set<ContentItem>()
            .Where(c => sectionIds.Contains(c.SectionId))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(i => i.Id).ToList();

        var itemTranslations = await dbContext.Set<ContentItemTranslation>()
            .Where(t => itemIds.Contains(t.ContentItemId))
            .ToListAsync(cancellationToken);

        // Quiz question/option text is visible to everyone regardless of ownership (it is not
        // part of the paywall below) — but IsCorrect is never queried into any DTO field here.
        var quizQuestions = await dbContext.Set<QuizQuestion>()
            .Where(q => itemIds.Contains(q.ContentItemId))
            .OrderBy(q => q.SortOrder)
            .ToListAsync(cancellationToken);
        var quizQuestionIds = quizQuestions.Select(q => q.Id).ToList();

        var quizQuestionTranslations = await dbContext.Set<QuizQuestionTranslation>()
            .Where(t => quizQuestionIds.Contains(t.QuizQuestionId))
            .ToListAsync(cancellationToken);

        var quizOptions = await dbContext.Set<QuizOption>()
            .Where(o => quizQuestionIds.Contains(o.QuizQuestionId))
            .OrderBy(o => o.SortOrder)
            .Select(o => new { o.Id, o.QuizQuestionId, o.SortOrder })
            .ToListAsync(cancellationToken);
        var quizOptionIds = quizOptions.Select(o => o.Id).ToList();

        var quizOptionTranslations = await dbContext.Set<QuizOptionTranslation>()
            .Where(t => quizOptionIds.Contains(t.QuizOptionId))
            .ToListAsync(cancellationToken);

        var activeOffer = await programOfferLookup.GetActiveOfferAsync(program.Id, cancellationToken);

        string? ownershipState = null;
        var isOwned = false;
        if (userId is not null)
        {
            isOwned = await programAccessContext.HasProgramAccessAsync(userId.Value, program.Id, cancellationToken);
            ownershipState = isOwned ? ClientProgramOwnershipState.Owned : ClientProgramOwnershipState.NotOwned;
        }

        var sectionDtos = sections.Select(section =>
        {
            var sectionResolution = TranslationResolver.Resolve(
                sectionTranslations.Where(t => t.SectionId == section.Id), requestedLanguage, program.DefaultLanguage);

            var itemDtos = items.Where(i => i.SectionId == section.Id).Select(item =>
            {
                var itemResolution = TranslationResolver.Resolve(
                    itemTranslations.Where(t => t.ContentItemId == item.Id), requestedLanguage, program.DefaultLanguage);

                // Quiz text/options are visible to everyone (not paywall-gated) — but IsCorrect is
                // never selected or mapped into this DTO, at any ownership state.
                IReadOnlyList<ClientQuizQuestionDto>? quizQuestionDtos = item.Type != ContentItemType.Quiz
                    ? null
                    : quizQuestions.Where(q => q.ContentItemId == item.Id).Select(question =>
                    {
                        var questionResolution = TranslationResolver.Resolve(
                            quizQuestionTranslations.Where(t => t.QuizQuestionId == question.Id), requestedLanguage, program.DefaultLanguage);

                        var optionDtos = quizOptions.Where(o => o.QuizQuestionId == question.Id).Select(option =>
                        {
                            var optionResolution = TranslationResolver.Resolve(
                                quizOptionTranslations.Where(t => t.QuizOptionId == option.Id), requestedLanguage, program.DefaultLanguage);

                            return new ClientQuizOptionDto(option.Id, option.SortOrder, optionResolution.Translation.Label);
                        }).ToList();

                        return new ClientQuizQuestionDto(question.Id, question.SortOrder, questionResolution.Translation.Text, optionDtos);
                    }).ToList();

                // Paywall: body text and media are only ever included for a confirmed owner.
                return new ClientContentItemDto(
                    item.Id,
                    item.Type.ToString(),
                    item.SortOrder,
                    item.IsRequired,
                    itemResolution.Translation.Title,
                    isOwned ? itemResolution.Translation.Body : null,
                    isOwned ? item.MediaAssetId : null,
                    quizQuestionDtos);
            }).ToList();

            return new ClientSectionDto(section.Id, section.SortOrder, sectionResolution.Translation.Title, sectionResolution.Translation.Description, itemDtos);
        }).ToList();

        return new ClientProgramDetailDto(
            program.Id,
            program.Slug,
            program.DomainId,
            programResolution.Translation.Title,
            programResolution.Translation.ShortDescription,
            programResolution.Translation.Description,
            program.CoverAssetId,
            activeOffer is null ? null : new ClientProgramOfferDto(activeOffer.Amount, activeOffer.Currency),
            ownershipState,
            sectionDtos);
    }
}
