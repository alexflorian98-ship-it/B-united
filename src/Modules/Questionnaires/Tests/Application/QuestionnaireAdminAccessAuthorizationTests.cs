using System.Net;
using System.Net.Http.Headers;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Questionnaires.Domain.Entities;
using BUnited.Modules.Questionnaires.Tests.TestSupport;

namespace BUnited.Modules.Questionnaires.Tests.Application;

/// <summary>P4.33.a — questionnaire submissions and guidance MUST be accessible only to the
/// submitting client and an explicitly authorized expert; administrators have no implicit access
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §6). Proves over real HTTP, through the actual
/// <c>ExpertQuestionnairesController</c>, that a caller lacking the specific
/// <c>questionnaire.review</c> permission claim — including a caller who otherwise looks like an
/// administrator (holds unrelated permissions) — is denied, while a caller who genuinely holds
/// that permission can read the submission. Only live-verified via curl before this test existed.</summary>
public sealed class QuestionnaireAdminAccessAuthorizationTests : IAsyncLifetime
{
    private QuestionnairesApiTestHost _host = null!;
    private Guid _submissionId;

    public async Task InitializeAsync()
    {
        _host = await QuestionnairesApiTestHost.StartAsync();

        var questionnaire = Questionnaire.Create(Guid.NewGuid(), "ro", createdBy: null);
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), questionnaire.Id);
        submission.Submit(DateTime.UtcNow);
        _submissionId = submission.Id;

        _host.DbContext.Set<Questionnaire>().Add(questionnaire);
        _host.DbContext.Set<QuestionnaireSubmission>().Add(submission);
        await _host.DbContext.SaveChangesAsync(CancellationToken.None);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Administrator_without_the_questionnaire_review_permission_cannot_read_a_submission()
    {
        // Represents an Administrator token holding unrelated permissions but never explicitly
        // granted questionnaire.review — the exact scenario docs/DEVELOPMENT_INSTRUCTIONS.md §6
        // forbids ("Administrators have no implicit access").
        var token = _host.IssueToken([WellKnownPermissionKeys.ChatModerate]);
        _host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _host.Client.GetAsync($"/api/v1/expert/questionnaires/submissions/{_submissionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_caller_cannot_read_a_submission()
    {
        _host.Client.DefaultRequestHeaders.Authorization = null;

        var response = await _host.Client.GetAsync($"/api/v1/expert/questionnaires/submissions/{_submissionId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expert_with_the_questionnaire_review_permission_can_read_the_submission()
    {
        var token = _host.IssueToken([WellKnownPermissionKeys.QuestionnaireReview]);
        _host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _host.Client.GetAsync($"/api/v1/expert/questionnaires/submissions/{_submissionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
