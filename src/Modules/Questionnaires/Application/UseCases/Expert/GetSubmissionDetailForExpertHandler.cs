using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

/// <summary>Opening a submission for the first time stamps <c>AssignedAt</c>/<c>ReviewedAt</c>
/// (§25–28 — retained even with a single expert, for response-time metrics) and produces a
/// <c>questionnaire.read</c> audit entry (P4.17) — every expert read of sensitive content is
/// audited, metadata-only (§35/§6).</summary>
public sealed class GetSubmissionDetailForExpertHandler(DbContext dbContext, IUserLookup userLookup, TimeProvider timeProvider, IAuditLogger auditLogger)
{
    public async Task<SubmissionDetailForExpertDto> HandleAsync(Guid submissionId, Guid actorId, CancellationToken cancellationToken)
    {
        var submission = await dbContext.Set<QuestionnaireSubmission>().SingleOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified submission does not exist.");

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        submission.MarkAssigned(utcNow);
        submission.MarkReviewed(utcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.QuestionnaireRead, actorUserId: actorId, entityType: "QuestionnaireSubmission", entityId: submissionId.ToString()),
            cancellationToken);

        var questionnaire = await dbContext.Set<Questionnaire>().SingleAsync(q => q.Id == submission.QuestionnaireId, cancellationToken);

        var questions = await dbContext.Set<Question>()
            .Where(q => q.QuestionnaireId == submission.QuestionnaireId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(cancellationToken);
        var questionIds = questions.Select(q => q.Id).ToList();

        var questionTranslations = await dbContext.Set<QuestionTranslation>()
            .Where(t => questionIds.Contains(t.QuestionId) && t.Language == questionnaire.DefaultLanguage)
            .ToListAsync(cancellationToken);

        var options = await dbContext.Set<QuestionOption>().Where(o => questionIds.Contains(o.QuestionId)).ToListAsync(cancellationToken);
        var optionIds = options.Select(o => o.Id).ToList();
        var optionTranslations = await dbContext.Set<QuestionOptionTranslation>()
            .Where(t => optionIds.Contains(t.QuestionOptionId) && t.Language == questionnaire.DefaultLanguage)
            .ToListAsync(cancellationToken);

        var answers = await dbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var qaPairs = questions.Select(question =>
        {
            var answer = answers.SingleOrDefault(a => a.QuestionId == question.Id);
            var text = questionTranslations.SingleOrDefault(t => t.QuestionId == question.Id)?.Text ?? string.Empty;
            var answerValue = answer?.Value ?? string.Empty;

            IReadOnlyList<string>? labels = null;
            if (question.Type is QuestionType.SingleChoice or QuestionType.MultiChoice or QuestionType.Scale && answer is not null)
            {
                var selectedValues = answer.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                labels = selectedValues
                    .Select(v => options.FirstOrDefault(o => o.QuestionId == question.Id && o.Value == v))
                    .Where(o => o is not null)
                    .Select(o => optionTranslations.SingleOrDefault(t => t.QuestionOptionId == o!.Id)?.Label ?? o!.Value)
                    .ToList();
            }

            return new QAPairDto(question.Id, text, question.Type.ToString(), answerValue, labels);
        }).ToList();

        var guidanceHistory = await dbContext.Set<GuidanceResponse>()
            .Where(g => g.QuestionnaireSubmissionId == submissionId)
            .OrderBy(g => g.Version)
            .ToListAsync(cancellationToken);
        var guidanceIds = guidanceHistory.Select(g => g.Id).ToList();

        var followUps = await dbContext.Set<GuidanceFollowUp>()
            .Where(f => guidanceIds.Contains(f.GuidanceResponseId))
            .ToListAsync(cancellationToken);

        var guidanceDtos = guidanceHistory.Select(g => new GuidanceVersionDto(
            g.Id,
            g.Version,
            g.Body,
            g.PublishedAt,
            followUps.Where(f => f.GuidanceResponseId == g.Id)
                .Select(f => new GuidanceFollowUpDto(f.Id, f.Question, f.Answer, f.AnsweredAt))
                .SingleOrDefault()))
            .ToList();

        var clientUser = await userLookup.GetByIdAsync(submission.UserId, cancellationToken);

        return new SubmissionDetailForExpertDto(
            submission.Id,
            submission.UserId,
            clientUser?.Email,
            submission.SubmittedAt,
            qaPairs,
            guidanceDtos);
    }
}
