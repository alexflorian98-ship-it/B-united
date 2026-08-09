using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class SubmitFollowUpHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task HandleAsync(SubmitFollowUpCommand command, CancellationToken cancellationToken)
    {
        var guidance = await dbContext.Set<GuidanceResponse>()
            .SingleOrDefaultAsync(g => g.Id == command.GuidanceResponseId, cancellationToken)
            ?? throw new NotFoundAppException("The specified guidance response does not exist.");

        var submission = await dbContext.Set<QuestionnaireSubmission>()
            .SingleAsync(s => s.Id == guidance.QuestionnaireSubmissionId, cancellationToken);

        if (submission.UserId != command.UserId)
        {
            throw new NotFoundAppException("The specified guidance response does not exist.");
        }

        var programId = await QuestionnaireProgramResolver.GetOwningProgramIdAsync(dbContext, submission.QuestionnaireId, cancellationToken);
        await programAccessContext.RequireProgramAccessAsync(command.UserId, programId, cancellationToken);

        if (guidance.PublishedAt is null)
        {
            throw new BusinessRuleAppException(
                "GUIDANCE_NOT_PUBLISHED",
                "errors.guidance.notPublished",
                "A follow-up can only be asked on published guidance.");
        }

        var alreadyExists = await dbContext.Set<GuidanceFollowUp>()
            .AnyAsync(f => f.GuidanceResponseId == command.GuidanceResponseId, cancellationToken);
        if (alreadyExists)
        {
            throw new BusinessRuleAppException(
                "GUIDANCE_FOLLOWUP_ALREADY_EXISTS",
                "errors.guidance.followUpAlreadyExists",
                "Only one bounded follow-up question is allowed per guidance response.");
        }

        dbContext.Set<GuidanceFollowUp>().Add(GuidanceFollowUp.Ask(command.GuidanceResponseId, command.Question));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
