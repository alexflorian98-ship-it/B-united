using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Client;

public sealed record SubmitQuizAnswerRequest(Guid QuestionId, Guid SelectedOptionId);

public sealed record SubmitQuizRequest(IReadOnlyList<SubmitQuizAnswerRequest> Answers);

public sealed record QuizAnswerInput(Guid QuestionId, Guid SelectedOptionId);

public sealed record SubmitQuizAttemptCommand(Guid ContentItemId, Guid UserId, IReadOnlyList<QuizAnswerInput> Answers);

/// <summary>Post-submission feedback intentionally reveals <see cref="CorrectOptionId"/> for each
/// question — this is normal quiz UX (showing the right answer after an attempt) and, unlike the
/// pre-submission read (<c>ClientQuizQuestionDto</c>), does not leak anything: the caller has
/// already committed their answer for this attempt by the time this result is produced.</summary>
public sealed record QuizQuestionResult(Guid QuestionId, bool WasCorrect, Guid CorrectOptionId);

public sealed record SubmitQuizAttemptResult(int CorrectCount, int TotalQuestions, IReadOnlyList<QuizQuestionResult> PerQuestionResults);

/// <summary>Grades server-side, never trusting a client-reported score (docs/DEVELOPMENT_INSTRUCTIONS.md
/// §6/§9). Resolves <c>ContentItem -> Section -> Program</c> server-side (never a client-supplied
/// program id) before gating on <see cref="IProgramAccessContext"/> — same pattern as
/// <see cref="GetVideoPlaybackHandler"/>. Every submitted <c>SelectedOptionId</c> is validated
/// against a join through <c>QuizQuestion.ContentItemId == contentItemId</c>, not looked up by
/// option id alone, so an option id belonging to a different content item's question is rejected
/// rather than silently mis-scored.</summary>
public sealed class SubmitQuizAttemptHandler(DbContext dbContext, IProgramAccessContext programAccessContext, TimeProvider timeProvider)
{
    public async Task<SubmitQuizAttemptResult> HandleAsync(SubmitQuizAttemptCommand command, CancellationToken cancellationToken)
    {
        var item = await dbContext.Set<ContentItem>().SingleOrDefaultAsync(c => c.Id == command.ContentItemId, cancellationToken)
            ?? throw new NotFoundAppException("The specified content item does not exist.");

        var section = await dbContext.Set<Section>().SingleAsync(s => s.Id == item.SectionId, cancellationToken);
        var program = await dbContext.Set<Program>().SingleAsync(p => p.Id == section.ProgramId, cancellationToken);

        await programAccessContext.RequireProgramAccessAsync(command.UserId, program.Id, cancellationToken);

        if (item.Type != ContentItemType.Quiz)
        {
            throw new BusinessRuleAppException("CONTENT_ITEM_NOT_A_QUIZ", "errors.contentItem.notAQuiz", "This content item is not a quiz.");
        }

        var questions = await dbContext.Set<QuizQuestion>()
            .Where(q => q.ContentItemId == command.ContentItemId)
            .ToListAsync(cancellationToken);
        var questionIds = questions.Select(q => q.Id).ToHashSet();

        if (questions.Count == 0)
        {
            throw new BusinessRuleAppException("QUIZ_HAS_NO_QUESTIONS", "errors.quiz.noQuestions", "This quiz has no questions to submit.");
        }

        // Every submitted question id must belong to THIS content item's quiz, and exactly one
        // answer per question — tampering (a different content item's question/option id, missing
        // questions, or duplicates) is rejected outright, not silently scored.
        var answeredQuestionIds = command.Answers.Select(a => a.QuestionId).ToList();
        if (answeredQuestionIds.Count != questionIds.Count
            || answeredQuestionIds.Distinct().Count() != answeredQuestionIds.Count
            || !questionIds.SetEquals(answeredQuestionIds))
        {
            throw new BusinessRuleAppException(
                "QUIZ_ANSWER_SET_MISMATCH",
                "errors.quiz.answerSetMismatch",
                "The submission must include exactly one answer for each of this quiz's current questions.");
        }

        // All options across THIS quiz's questions — joined through QuizQuestion.ContentItemId,
        // never looked up by option id alone, so an option from a different content item's
        // question cannot be substituted in to game the scoring.
        var options = await dbContext.Set<QuizOption>()
            .Where(o => questionIds.Contains(o.QuizQuestionId))
            .ToListAsync(cancellationToken);

        var results = new List<QuizQuestionResult>();
        var correctCount = 0;

        foreach (var answer in command.Answers)
        {
            var correctOption = options.SingleOrDefault(o => o.QuizQuestionId == answer.QuestionId && o.IsCorrect)
                ?? throw new BusinessRuleAppException(
                    "QUIZ_QUESTION_HAS_NO_CORRECT_OPTION",
                    "errors.quiz.questionHasNoCorrectOption",
                    "This quiz question has no correct option configured and cannot be graded.");

            var selectedOptionBelongsToQuestion = options.Any(
                o => o.Id == answer.SelectedOptionId && o.QuizQuestionId == answer.QuestionId);
            if (!selectedOptionBelongsToQuestion)
            {
                throw new BusinessRuleAppException(
                    "QUIZ_ANSWER_OPTION_INVALID",
                    "errors.quiz.answerOptionInvalid",
                    "The selected option does not belong to the specified question.");
            }

            var wasCorrect = answer.SelectedOptionId == correctOption.Id;
            if (wasCorrect)
            {
                correctCount++;
            }

            results.Add(new QuizQuestionResult(answer.QuestionId, wasCorrect, correctOption.Id));
        }

        dbContext.Set<QuizAttempt>().Add(
            QuizAttempt.Create(command.UserId, command.ContentItemId, questions.Count, correctCount, timeProvider.GetUtcNow().UtcDateTime));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitQuizAttemptResult(correctCount, questions.Count, results);
    }
}
