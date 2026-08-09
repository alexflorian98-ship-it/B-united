using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

/// <summary>Guidance text is high-sensitivity data (CLAUDE.md) — gated on ownership-by-<c>UserId</c>
/// and program entitlement (ADR-003) alike, matching every other single-resource read here.</summary>
public sealed class GetGuidanceHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<GuidanceDto?> HandleAsync(Guid userId, Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await dbContext.Set<QuestionnaireSubmission>()
            .SingleOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified submission does not exist.");

        if (submission.UserId != userId)
        {
            throw new NotFoundAppException("The specified submission does not exist.");
        }

        var programId = await QuestionnaireProgramResolver.GetOwningProgramIdAsync(dbContext, submission.QuestionnaireId, cancellationToken);
        await programAccessContext.RequireProgramAccessAsync(userId, programId, cancellationToken);

        var latestPublished = await dbContext.Set<GuidanceResponse>()
            .Where(g => g.QuestionnaireSubmissionId == submissionId && g.PublishedAt != null)
            .OrderByDescending(g => g.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestPublished is null)
        {
            return null;
        }

        var followUp = await dbContext.Set<GuidanceFollowUp>()
            .Where(f => f.GuidanceResponseId == latestPublished.Id)
            .Select(f => new GuidanceFollowUpDto(f.Id, f.Question, f.Answer, f.AnsweredAt))
            .SingleOrDefaultAsync(cancellationToken);

        return new GuidanceDto(latestPublished.Id, submission.Id, latestPublished.Version, latestPublished.Body, latestPublished.PublishedAt!.Value, followUp);
    }
}
