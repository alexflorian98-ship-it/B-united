using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

/// <summary>docs/PROMPT.md §25–28 expert queue: sorted oldest-submitted-first (the metric
/// that matters — "oldest unanswered submission"), with a server-computed waiting-time bucket
/// (P4.10.b) so every client renders the same bucket boundaries.</summary>
public sealed class GetQueueHandler(DbContext dbContext, IUserLookup userLookup, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<QueueItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var submissions = await dbContext.Set<QuestionnaireSubmission>()
            .Where(s => s.Status == QuestionnaireSubmissionStatus.Submitted)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        var users = await userLookup.GetByIdsAsync(submissions.Select(s => s.UserId).Distinct().ToList(), cancellationToken);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        return submissions.Select(s =>
        {
            var waitingHours = (utcNow - s.SubmittedAt!.Value).TotalHours;
            var bucket = waitingHours switch
            {
                < 24 => WaitingTimeBucket.Under24Hours,
                < 48 => WaitingTimeBucket.Between24And48Hours,
                _ => WaitingTimeBucket.Over48Hours,
            };

            return new QueueItemDto(
                s.Id,
                s.UserId,
                users.TryGetValue(s.UserId, out var user) ? user.Email : null,
                s.SubmittedAt!.Value,
                bucket,
                s.Status.ToString());
        }).ToList();
    }
}
