using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class GetProgramDetailHandler(DbContext dbContext)
{
    public async Task<ProgramDetailDto> HandleAsync(Guid programId, CancellationToken cancellationToken)
    {
        var program = await dbContext.Set<Program>().SingleOrDefaultAsync(p => p.Id == programId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist.");

        var translations = await dbContext.Set<ProgramTranslation>()
            .Where(t => t.ProgramId == programId)
            .Select(t => new ProgramTranslationDto(t.Language, t.Title, t.ShortDescription, t.Description))
            .ToListAsync(cancellationToken);

        var sections = await dbContext.Set<Section>()
            .Where(s => s.ProgramId == programId)
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

        // Quiz data (admin-only — includes IsCorrect, unlike the client-facing read) for whichever
        // items are actually quizzes; empty queries are cheap no-ops for programs with none.
        var quizItemIds = items.Where(i => i.Type == ContentItemType.Quiz).Select(i => i.Id).ToList();
        var quizQuestions = await dbContext.Set<QuizQuestion>()
            .Where(q => quizItemIds.Contains(q.ContentItemId))
            .OrderBy(q => q.SortOrder)
            .ToListAsync(cancellationToken);
        var quizQuestionIds = quizQuestions.Select(q => q.Id).ToList();

        var quizQuestionTranslations = await dbContext.Set<QuizQuestionTranslation>()
            .Where(t => quizQuestionIds.Contains(t.QuizQuestionId))
            .ToListAsync(cancellationToken);

        var quizOptions = await dbContext.Set<QuizOption>()
            .Where(o => quizQuestionIds.Contains(o.QuizQuestionId))
            .OrderBy(o => o.SortOrder)
            .ToListAsync(cancellationToken);
        var quizOptionIds = quizOptions.Select(o => o.Id).ToList();

        var quizOptionTranslations = await dbContext.Set<QuizOptionTranslation>()
            .Where(t => quizOptionIds.Contains(t.QuizOptionId))
            .ToListAsync(cancellationToken);

        List<AdminQuizQuestionDetailDto>? BuildQuizQuestions(Guid contentItemId) =>
            quizQuestions.Where(q => q.ContentItemId == contentItemId)
                .Select(question => new AdminQuizQuestionDetailDto(
                    question.Id,
                    question.SortOrder,
                    quizQuestionTranslations.Where(t => t.QuizQuestionId == question.Id)
                        .Select(t => new AdminQuizQuestionTranslationDto(t.Language, t.Text))
                        .ToList(),
                    quizOptions.Where(o => o.QuizQuestionId == question.Id)
                        .Select(option => new AdminQuizOptionDetailDto(
                            option.Id,
                            option.SortOrder,
                            option.IsCorrect,
                            quizOptionTranslations.Where(t => t.QuizOptionId == option.Id)
                                .Select(t => new AdminQuizOptionTranslationDto(t.Language, t.Label))
                                .ToList()))
                        .ToList()))
                .ToList();

        var sectionDtos = sections.Select(section => new SectionDetailDto(
            section.Id,
            section.SortOrder,
            section.Status,
            sectionTranslations.Where(t => t.SectionId == section.Id)
                .Select(t => new SectionTranslationDto(t.Language, t.Title, t.Description))
                .ToList(),
            items.Where(i => i.SectionId == section.Id)
                .Select(item => new ContentItemDetailDto(
                    item.Id,
                    item.Type,
                    item.SortOrder,
                    item.IsRequired,
                    item.MediaAssetId,
                    itemTranslations.Where(t => t.ContentItemId == item.Id)
                        .Select(t => new ContentItemTranslationDto(t.Language, t.Title, t.Body))
                        .ToList(),
                    item.Type == ContentItemType.Quiz ? BuildQuizQuestions(item.Id) : null))
                .ToList()))
            .ToList();

        return new ProgramDetailDto(
            program.Id,
            program.DomainId,
            program.Slug,
            program.Status,
            program.DefaultLanguage,
            program.CoverAssetId,
            program.SortOrder,
            translations,
            sectionDtos);
    }
}
