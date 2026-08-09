using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

/// <summary>Ownership-by-<c>UserId</c> and program entitlement (ADR-003) are both required —
/// neither replaces the other. Ownership is checked first (returns the identical "not found"
/// shape as a non-existent id, never confirming another user's resource exists); entitlement is
/// then enforced via <see cref="IProgramAccessContext"/>, matching Progress's identical
/// precedent for reading a single caller-owned resource.</summary>
public sealed class GetMySubmissionHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<MySubmissionDto> HandleAsync(Guid userId, Guid submissionId, CancellationToken cancellationToken)
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

        var answers = await dbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == submissionId)
            .Select(a => new SubmissionAnswerDto(a.QuestionId, a.Value))
            .ToListAsync(cancellationToken);

        return new MySubmissionDto(submission.Id, submission.QuestionnaireId, submission.Status.ToString(), submission.StartedAt, submission.SubmittedAt, answers);
    }
}
