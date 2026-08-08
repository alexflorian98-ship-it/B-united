using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Notifications.Contracts;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

public sealed class PublishGuidanceHandler(
    DbContext dbContext,
    TimeProvider timeProvider,
    IAuditLogger auditLogger,
    IUserLookup userLookup,
    INotificationSender notificationSender)
{
    public async Task HandleAsync(Guid guidanceResponseId, Guid actorId, CancellationToken cancellationToken)
    {
        var guidance = await dbContext.Set<GuidanceResponse>().SingleOrDefaultAsync(g => g.Id == guidanceResponseId, cancellationToken)
            ?? throw new NotFoundAppException("The specified guidance response does not exist.");

        var submission = await dbContext.Set<QuestionnaireSubmission>().SingleAsync(s => s.Id == guidance.QuestionnaireSubmissionId, cancellationToken);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            guidance.Publish(utcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleAppException("GUIDANCE_ALREADY_PUBLISHED", "errors.guidance.alreadyPublished", ex.Message);
        }

        // A resubmitted follow-up round (submission already Answered from an earlier guidance
        // version) does not re-transition submission status — only the first publish does.
        if (submission.Status == Domain.QuestionnaireSubmissionStatus.Submitted)
        {
            submission.MarkAnswered(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.GuidancePublished, actorUserId: actorId, entityType: "GuidanceResponse", entityId: guidance.Id.ToString()),
            cancellationToken);

        var client = await userLookup.GetByIdAsync(submission.UserId, cancellationToken);
        if (client is not null)
        {
            await notificationSender.SendAsync(
                NotificationType.GuidancePublished,
                client.Email,
                new Dictionary<string, string> { ["submissionId"] = submission.Id.ToString() },
                cancellationToken);
        }
    }
}
