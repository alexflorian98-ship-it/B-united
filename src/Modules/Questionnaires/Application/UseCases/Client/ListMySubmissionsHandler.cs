using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class ListMySubmissionsHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<MySubmissionDto>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var submissions = await dbContext.Set<QuestionnaireSubmission>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return submissions
            .Select(s => new MySubmissionDto(s.Id, s.QuestionnaireId, s.Status.ToString(), s.StartedAt, s.SubmittedAt, Answers: []))
            .ToList();
    }
}
