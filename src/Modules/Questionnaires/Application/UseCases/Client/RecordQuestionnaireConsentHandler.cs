using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Client;

public sealed class RecordQuestionnaireConsentHandler(IConsentContext consentContext)
{
    public Task HandleAsync(Guid userId, CancellationToken cancellationToken) =>
        consentContext.RecordConsentAsync(userId, QuestionnaireConsent.ConsentType, QuestionnaireConsent.CurrentVersion, cancellationToken);
}
