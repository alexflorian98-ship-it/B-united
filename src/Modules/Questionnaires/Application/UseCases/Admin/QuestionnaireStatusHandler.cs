using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class QuestionnaireStatusHandler(DbContext dbContext)
{
    public Task PublishAsync(Guid questionnaireId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(questionnaireId, q => q.Publish(actorId), cancellationToken);

    public Task UnpublishAsync(Guid questionnaireId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(questionnaireId, q => q.Unpublish(actorId), cancellationToken);

    public Task ArchiveAsync(Guid questionnaireId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(questionnaireId, q => q.Archive(actorId), cancellationToken);

    private async Task TransitionAsync(Guid questionnaireId, Action<Questionnaire> transition, CancellationToken cancellationToken)
    {
        var questionnaire = await dbContext.Set<Questionnaire>().SingleOrDefaultAsync(q => q.Id == questionnaireId, cancellationToken)
            ?? throw new NotFoundAppException("The specified questionnaire does not exist.");

        try
        {
            transition(questionnaire);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleAppException("QUESTIONNAIRE_STATUS_TRANSITION_INVALID", "errors.questionnaire.invalidStatusTransition", ex.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
