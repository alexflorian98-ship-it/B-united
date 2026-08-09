using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

/// <summary>Lists the caller's own submissions across every questionnaire they've started.
/// Unlike the single-resource handlers (<see cref="GetMySubmissionHandler"/> et al.), a denied
/// program here does not throw — it is silently filtered out of the list, mirroring Progress's
/// <c>GetContentProgressHandler</c> precedent for multi-row reads (a list endpoint should not
/// fail entirely because one row's program access was later revoked, e.g. via refund). Program
/// checks are deduplicated per distinct <c>ProgramId</c> to avoid redundant lookups.</summary>
public sealed class ListMySubmissionsHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<IReadOnlyList<MySubmissionDto>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var submissions = await dbContext.Set<QuestionnaireSubmission>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var questionnaireIds = submissions.Select(s => s.QuestionnaireId).Distinct().ToList();
        var programIdByQuestionnaireId = await dbContext.Set<Questionnaire>()
            .Where(q => questionnaireIds.Contains(q.Id))
            .Select(q => new { q.Id, q.ProgramId })
            .ToDictionaryAsync(x => x.Id, x => x.ProgramId, cancellationToken);

        var authorizedProgramIds = new HashSet<Guid>();
        var deniedProgramIds = new HashSet<Guid>();
        var result = new List<MySubmissionDto>();

        foreach (var submission in submissions)
        {
            if (!programIdByQuestionnaireId.TryGetValue(submission.QuestionnaireId, out var programId))
            {
                // Orphaned questionnaire reference (deleted template) — nothing authoritative to
                // gate against, so it is skipped rather than shown or throwing.
                continue;
            }

            if (!authorizedProgramIds.Contains(programId))
            {
                if (deniedProgramIds.Contains(programId))
                {
                    continue;
                }

                if (!await programAccessContext.HasProgramAccessAsync(userId, programId, cancellationToken))
                {
                    deniedProgramIds.Add(programId);
                    continue;
                }

                authorizedProgramIds.Add(programId);
            }

            result.Add(new MySubmissionDto(submission.Id, submission.QuestionnaireId, submission.Status.ToString(), submission.StartedAt, submission.SubmittedAt, Answers: []));
        }

        return result;
    }
}
