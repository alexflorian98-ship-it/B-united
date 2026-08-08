using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

public sealed class AnswerFollowUpHandler(DbContext dbContext, TimeProvider timeProvider)
{
    public async Task HandleAsync(AnswerFollowUpCommand command, CancellationToken cancellationToken)
    {
        var followUp = await dbContext.Set<GuidanceFollowUp>().SingleOrDefaultAsync(f => f.Id == command.FollowUpId, cancellationToken)
            ?? throw new NotFoundAppException("The specified follow-up does not exist.");

        try
        {
            followUp.Respond(command.Answer, timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleAppException("GUIDANCE_FOLLOWUP_ALREADY_ANSWERED", "errors.guidance.followUpAlreadyAnswered", ex.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
