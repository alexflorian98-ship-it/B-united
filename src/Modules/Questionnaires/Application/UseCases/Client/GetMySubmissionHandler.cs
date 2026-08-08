using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class GetMySubmissionHandler(DbContext dbContext)
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

        var answers = await dbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == submissionId)
            .Select(a => new SubmissionAnswerDto(a.QuestionId, a.Value))
            .ToListAsync(cancellationToken);

        return new MySubmissionDto(submission.Id, submission.QuestionnaireId, submission.Status.ToString(), submission.StartedAt, submission.SubmittedAt, answers);
    }
}
