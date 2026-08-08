using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class SubmitQuestionnaireHandler(DbContext dbContext, TimeProvider timeProvider, IAuditLogger auditLogger)
{
    public async Task HandleAsync(Guid userId, Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await dbContext.Set<QuestionnaireSubmission>()
            .SingleOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified submission does not exist.");

        if (submission.UserId != userId)
        {
            throw new NotFoundAppException("The specified submission does not exist.");
        }

        if (submission.Status != QuestionnaireSubmissionStatus.Draft)
        {
            throw new BusinessRuleAppException(
                "QUESTIONNAIRE_SUBMISSION_NOT_DRAFT",
                "errors.questionnaire.submissionNotDraft",
                "Only a draft submission can be submitted.");
        }

        var requiredQuestionIds = await dbContext.Set<Question>()
            .Where(q => q.QuestionnaireId == submission.QuestionnaireId && q.IsRequired)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        var answeredQuestionIds = await dbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == submissionId && a.Value != "")
            .Select(a => a.QuestionId)
            .ToListAsync(cancellationToken);

        var missing = requiredQuestionIds.Except(answeredQuestionIds).ToList();
        if (missing.Count > 0)
        {
            throw new BusinessRuleAppException(
                "QUESTIONNAIRE_REQUIRED_ANSWERS_MISSING",
                "errors.questionnaire.requiredAnswersMissing",
                $"{missing.Count} required question(s) are not yet answered.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        submission.Submit(utcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Notification to the reviewing expert is not wired yet: Identity has no concept of "the
        // primary expert's email", only permission-based access — see docs/TASKS.md P4.13 for the
        // documented gap. The submission itself is fully persisted and visible in the queue either way.
        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.QuestionnaireSubmitted, actorUserId: userId, entityType: "QuestionnaireSubmission", entityId: submissionId.ToString()),
            cancellationToken);
    }
}
