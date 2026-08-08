using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

/// <summary>docs/PROMPT.md §35/P4.14: consent must be captured before questionnaire start.
/// Idempotent: a second call for the same user+questionnaire returns the existing Draft
/// submission instead of creating a duplicate.</summary>
public sealed class StartOrResumeSubmissionHandler(DbContext dbContext, IConsentContext consentContext)
{
    public async Task<Guid> HandleAsync(Guid userId, Guid questionnaireId, CancellationToken cancellationToken)
    {
        var hasConsented = await consentContext.HasConsentedAsync(
            userId, QuestionnaireConsent.ConsentType, QuestionnaireConsent.CurrentVersion, cancellationToken);
        if (!hasConsented)
        {
            throw new BusinessRuleAppException(
                "QUESTIONNAIRE_CONSENT_REQUIRED",
                "errors.questionnaire.consentRequired",
                "Consent to questionnaire data processing is required before starting a questionnaire.");
        }

        var questionnaireExists = await dbContext.Set<Questionnaire>()
            .AnyAsync(q => q.Id == questionnaireId && q.Status == QuestionnaireStatus.Published, cancellationToken);
        if (!questionnaireExists)
        {
            throw new NotFoundAppException("The specified questionnaire does not exist or is not published.");
        }

        var existingDraft = await dbContext.Set<QuestionnaireSubmission>()
            .SingleOrDefaultAsync(
                s => s.UserId == userId && s.QuestionnaireId == questionnaireId && s.Status == QuestionnaireSubmissionStatus.Draft,
                cancellationToken);
        if (existingDraft is not null)
        {
            return existingDraft.Id;
        }

        var submission = QuestionnaireSubmission.Start(userId, questionnaireId);
        dbContext.Set<QuestionnaireSubmission>().Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);

        return submission.Id;
    }
}
