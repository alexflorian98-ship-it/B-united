using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Questionnaires.Application;
using BUnited.Modules.Questionnaires.Application.UseCases.Admin;
using BUnited.Modules.Questionnaires.Application.UseCases.Client;
using BUnited.Modules.Questionnaires.Application.UseCases.Expert;
using BUnited.Modules.Questionnaires.Domain.Entities;
using BUnited.Modules.Questionnaires.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Tests.Security;

/// <summary>Security-gap-closure item #1 (two-user authenticated IDOR suite): questionnaire
/// submissions, draft answers, guidance, and follow-ups are the single most sensitive data
/// category in the product (CLAUDE.md: "Administrators have no implicit access", ADR-006). Every
/// handler already enforces ownership-by-UserId (see GetGuidanceHandler, SaveDraftAnswersHandler,
/// SubmitQuestionnaireHandler, SubmitFollowUpHandler) and is unit-tested for it
/// (QuestionnaireFlowTests.A_users_submission_is_invisible_to_another_user) — this suite instead
/// drives the REAL controllers over real HTTP behind the real JWT + authorization pipeline, the
/// same rationale BillingCrossUserAccessTests documents: a routing/DI-wiring mistake (wrong claim
/// read, wrong route binding) would not be caught by a handler-only test. "otherUserId" is also
/// granted program access in every case below specifically to prove the 404 is an OWNERSHIP
/// decision, not an entitlement decision — if entitlement were the only gate, a same-program
/// user could read another client's guidance.</summary>
public sealed class QuestionnaireCrossUserAccessTests
{
    private async Task<(QuestionnairesApiTestHost Host, Guid OwnerId, Guid OtherUserId, Guid SubmissionId, Guid GuidanceResponseId, Guid QuestionId)> SeedSubmittedQuestionnaireWithGuidanceAsync()
    {
        var host = await QuestionnairesApiTestHost.StartAsync();
        var expertId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        // Both users own the program — isolates the assertions below to prove the denial is
        // ownership-based (UserId), not entitlement-based (program access).
        host.ProgramAccess.GrantAccess(ownerId, programId);
        host.ProgramAccess.GrantAccess(otherUserId, programId);
        await host.Consent.RecordConsentAsync(ownerId, QuestionnaireConsent.ConsentType, QuestionnaireConsent.CurrentVersion, CancellationToken.None);
        await host.Consent.RecordConsentAsync(otherUserId, QuestionnaireConsent.ConsentType, QuestionnaireConsent.CurrentVersion, CancellationToken.None);

        var programLookup = new FakeProgramLookup();
        programLookup.AddProgram(programId);
        var questionnaireId = await new CreateQuestionnaireHandler(host.DbContext, programLookup)
            .HandleAsync(new CreateQuestionnaireCommand(programId, "ro", "Chestionar", "Descriere", expertId), CancellationToken.None);
        var questionId = await new AddQuestionHandler(host.DbContext)
            .HandleAsync(new AddQuestionCommand(questionnaireId, "Text", true, "Ce te aduce aici?", null, expertId), CancellationToken.None);
        await new QuestionnaireStatusHandler(host.DbContext).PublishAsync(questionnaireId, expertId, CancellationToken.None);

        var ownerToken = host.IssueToken(ownerId, [WellKnownPermissionKeys.QuestionnaireSubmit]);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var startResponse = await host.Client.PostAsync($"/api/v1/questionnaires/{questionnaireId}/start", null);
        var submissionId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        var saveResponse = await host.Client.PutAsJsonAsync(
            $"/api/v1/questionnaires/submissions/{submissionId}/answers",
            new { Answers = new[] { new { QuestionId = questionId, Value = "Vreau ghidare." } } });
        saveResponse.EnsureSuccessStatusCode();

        var submitResponse = await host.Client.PostAsync($"/api/v1/questionnaires/submissions/{submissionId}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var guidanceId = await new SaveGuidanceDraftHandler(host.DbContext)
            .HandleAsync(new SaveGuidanceDraftCommand(submissionId, "Recomandarea mea.", expertId), CancellationToken.None);
        await new PublishGuidanceHandler(host.DbContext, TimeProvider.System, host.AuditLogger, new FakeUserLookup(), new FakeNotificationSender())
            .HandleAsync(guidanceId, expertId, CancellationToken.None);

        host.Client.DefaultRequestHeaders.Authorization = null;
        return (host, ownerId, otherUserId, submissionId, guidanceId, questionId);
    }

    [Fact]
    public async Task GetSubmission_returns_404_for_another_users_submission_and_200_for_the_owner()
    {
        var (host, ownerId, otherUserId, submissionId, _, _) = await SeedSubmittedQuestionnaireWithGuidanceAsync();
        await using var _ = host;

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(otherUserId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var otherResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{submissionId}");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);

        var randomIdResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, randomIdResponse.StatusCode);

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(ownerId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var ownerResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{submissionId}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task SaveDraftAnswers_returns_404_for_another_users_submission_and_persists_no_answer()
    {
        var (host, _, otherUserId, submissionId, _, questionId) = await SeedSubmittedQuestionnaireWithGuidanceAsync();
        await using var _ = host;

        var answersBeforeAttack = await host.DbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == submissionId).ToListAsync();

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(otherUserId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var response = await host.Client.PutAsJsonAsync(
            $"/api/v1/questionnaires/submissions/{submissionId}/answers",
            new { Answers = new[] { new { QuestionId = questionId, Value = "Injected by attacker." } } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var answersAfterAttack = await host.DbContext.Set<QuestionnaireAnswer>()
            .Where(a => a.QuestionnaireSubmissionId == submissionId).ToListAsync();
        Assert.Equal(answersBeforeAttack.Select(a => a.Value), answersAfterAttack.Select(a => a.Value));
        Assert.DoesNotContain(answersAfterAttack, a => a.Value == "Injected by attacker.");
    }

    [Fact]
    public async Task Submit_returns_404_for_another_users_submission_and_leaves_its_status_untouched()
    {
        var (host, _, otherUserId, submissionId, _, _) = await SeedSubmittedQuestionnaireWithGuidanceAsync();
        await using var _ = host;

        var statusBeforeAttack = (await host.DbContext.Set<QuestionnaireSubmission>().SingleAsync(s => s.Id == submissionId)).Status;

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(otherUserId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var response = await host.Client.PostAsync($"/api/v1/questionnaires/submissions/{submissionId}/submit", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var statusAfterAttack = (await host.DbContext.Set<QuestionnaireSubmission>().SingleAsync(s => s.Id == submissionId)).Status;
        Assert.Equal(statusBeforeAttack, statusAfterAttack);
    }

    [Fact]
    public async Task GetGuidance_returns_404_for_another_users_submission_and_200_with_the_guidance_body_for_the_owner()
    {
        var (host, ownerId, otherUserId, submissionId, _, _) = await SeedSubmittedQuestionnaireWithGuidanceAsync();
        await using var _ = host;

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(otherUserId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var otherResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{submissionId}/guidance");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.DoesNotContain("Recomandarea mea.", await otherResponse.Content.ReadAsStringAsync());

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(ownerId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var ownerResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{submissionId}/guidance");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Contains("Recomandarea mea.", await ownerResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SubmitFollowUp_returns_404_for_another_users_guidance_and_creates_no_follow_up_row()
    {
        var (host, _, otherUserId, _, guidanceResponseId, _) = await SeedSubmittedQuestionnaireWithGuidanceAsync();
        await using var _ = host;

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", host.IssueToken(otherUserId, [WellKnownPermissionKeys.QuestionnaireSubmit]));
        var response = await host.Client.PostAsJsonAsync(
            $"/api/v1/questionnaires/guidance/{guidanceResponseId}/follow-up",
            new { Question = "Injected follow-up from attacker." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var followUps = await host.DbContext.Set<GuidanceFollowUp>()
            .Where(f => f.GuidanceResponseId == guidanceResponseId).ToListAsync();
        Assert.Empty(followUps);
    }

    [Fact]
    public async Task GetSubmission_and_GetGuidance_return_401_when_unauthenticated()
    {
        var (host, _, _, submissionId, _, _) = await SeedSubmittedQuestionnaireWithGuidanceAsync();
        await using var _ = host;

        var submissionResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{submissionId}");
        Assert.Equal(HttpStatusCode.Unauthorized, submissionResponse.StatusCode);

        var guidanceResponse = await host.Client.GetAsync($"/api/v1/questionnaires/submissions/{submissionId}/guidance");
        Assert.Equal(HttpStatusCode.Unauthorized, guidanceResponse.StatusCode);
    }
}
