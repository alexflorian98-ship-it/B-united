using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class SaveDraftAnswersHandler(DbContext dbContext, TimeProvider timeProvider, IProgramAccessContext programAccessContext)
{
    public async Task HandleAsync(SaveDraftAnswersCommand command, CancellationToken cancellationToken)
    {
        var submission = await dbContext.Set<QuestionnaireSubmission>()
            .SingleOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified submission does not exist.");

        if (submission.UserId != command.UserId)
        {
            // Deliberately the same NotFound shape as "doesn't exist" — never confirm existence
            // of another user's submission to an unauthorized caller (§35 restrictive authorization).
            throw new NotFoundAppException("The specified submission does not exist.");
        }

        var programId = await QuestionnaireProgramResolver.GetOwningProgramIdAsync(dbContext, submission.QuestionnaireId, cancellationToken);
        await programAccessContext.RequireProgramAccessAsync(command.UserId, programId, cancellationToken);

        if (submission.Status != QuestionnaireSubmissionStatus.Draft)
        {
            throw new BusinessRuleAppException(
                "QUESTIONNAIRE_SUBMISSION_NOT_DRAFT",
                "errors.questionnaire.submissionNotDraft",
                "Only a draft submission can be updated.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        submission.MarkStarted(utcNow);

        var questionIds = command.Answers.Select(a => a.QuestionId).ToList();
        var existingAnswers = await dbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == command.SubmissionId && questionIds.Contains(a.QuestionId))
            .ToListAsync(cancellationToken);

        foreach (var input in command.Answers)
        {
            var existing = existingAnswers.SingleOrDefault(a => a.QuestionId == input.QuestionId);
            if (existing is null)
            {
                dbContext.Set<QuestionnaireAnswer>().Add(QuestionnaireAnswer.Create(command.SubmissionId, input.QuestionId, input.Value));
            }
            else
            {
                existing.UpdateValue(input.Value);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
