using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class ExportMyQuestionnaireDataHandler(DbContext dbContext, TimeProvider timeProvider)
{
    public async Task<QuestionnaireDataExportDto> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var submissions = await dbContext.Set<QuestionnaireSubmission>()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
        var submissionIds = submissions.Select(s => s.Id).ToList();

        var answers = await dbContext.Set<QuestionnaireAnswer>()
            .Where(a => submissionIds.Contains(a.QuestionnaireSubmissionId))
            .ToListAsync(cancellationToken);

        var guidance = await dbContext.Set<GuidanceResponse>()
            .Where(g => submissionIds.Contains(g.QuestionnaireSubmissionId))
            .ToListAsync(cancellationToken);

        var exportedSubmissions = submissions.Select(submission => new ExportedSubmissionDto(
            submission.Id,
            submission.QuestionnaireId,
            submission.Status.ToString(),
            submission.CreatedAt,
            submission.StartedAt,
            submission.SubmittedAt,
            answers.Where(a => a.QuestionnaireSubmissionId == submission.Id)
                .Select(a => new ExportedAnswerDto(a.QuestionId, a.Value))
                .ToList(),
            guidance.Where(g => g.QuestionnaireSubmissionId == submission.Id)
                .Select(g => new ExportedGuidanceDto(g.Version, g.Body, g.PublishedAt))
                .ToList()))
            .ToList();

        return new QuestionnaireDataExportDto(userId, timeProvider.GetUtcNow().UtcDateTime, exportedSubmissions);
    }
}
