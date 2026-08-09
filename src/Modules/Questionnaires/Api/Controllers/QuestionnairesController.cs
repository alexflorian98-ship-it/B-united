using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Application.UseCases.Client;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Questionnaires.Api.Controllers;

/// <summary>Client-facing questionnaire flow (§25–28/§35). Every read/write is scoped to the
/// authenticated caller's own data — resource-ownership checks live in the handlers themselves
/// (P4.15), not just here, so a submission id belonging to another user always resolves as
/// "not found," never a 403 that would confirm the resource exists.</summary>
[ApiController]
[Route("api/v1/questionnaires")]
[Authorize(Policy = WellKnownPermissionKeys.QuestionnaireSubmit)]
public sealed class QuestionnairesController(
    RecordQuestionnaireConsentHandler recordConsentHandler,
    ListPublishedQuestionnairesHandler listPublishedQuestionnairesHandler,
    GetClientQuestionnaireHandler getClientQuestionnaireHandler,
    StartOrResumeSubmissionHandler startOrResumeSubmissionHandler,
    SaveDraftAnswersHandler saveDraftAnswersHandler,
    GetMySubmissionHandler getMySubmissionHandler,
    ListMySubmissionsHandler listMySubmissionsHandler,
    SubmitQuestionnaireHandler submitQuestionnaireHandler,
    GetGuidanceHandler getGuidanceHandler,
    SubmitFollowUpHandler submitFollowUpHandler,
    ExportMyQuestionnaireDataHandler exportMyQuestionnaireDataHandler) : ControllerBase
{
    [HttpPost("consent")]
    public async Task<IActionResult> RecordConsent(CancellationToken cancellationToken)
    {
        await recordConsentHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublishedQuestionnaireSummaryDto>>> ListPublished([FromQuery] string language, CancellationToken cancellationToken)
    {
        var result = await listPublishedQuestionnairesHandler.HandleAsync(language, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{questionnaireId:guid}")]
    public async Task<ActionResult<ClientQuestionnaireDto>> Get(Guid questionnaireId, [FromQuery] string language, CancellationToken cancellationToken)
    {
        var result = await getClientQuestionnaireHandler.HandleAsync(User.GetUserId(), questionnaireId, language, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{questionnaireId:guid}/start")]
    public async Task<ActionResult<Guid>> Start(Guid questionnaireId, CancellationToken cancellationToken)
    {
        var submissionId = await startOrResumeSubmissionHandler.HandleAsync(User.GetUserId(), questionnaireId, cancellationToken);
        return Ok(submissionId);
    }

    [HttpGet("submissions")]
    public async Task<ActionResult<IReadOnlyList<MySubmissionDto>>> ListMySubmissions(CancellationToken cancellationToken)
    {
        var result = await listMySubmissionsHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<ActionResult<MySubmissionDto>> GetSubmission(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await getMySubmissionHandler.HandleAsync(User.GetUserId(), submissionId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("submissions/{submissionId:guid}/answers")]
    public async Task<IActionResult> SaveDraftAnswers(Guid submissionId, SaveDraftAnswersRequest request, CancellationToken cancellationToken)
    {
        var answers = request.Answers.Select(a => new AnswerInput(a.QuestionId, a.Value)).ToList();
        await saveDraftAnswersHandler.HandleAsync(new SaveDraftAnswersCommand(User.GetUserId(), submissionId, answers), cancellationToken);
        return NoContent();
    }

    [HttpPost("submissions/{submissionId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid submissionId, CancellationToken cancellationToken)
    {
        await submitQuestionnaireHandler.HandleAsync(User.GetUserId(), submissionId, cancellationToken);
        return NoContent();
    }

    [HttpGet("submissions/{submissionId:guid}/guidance")]
    public async Task<ActionResult<GuidanceDto?>> GetGuidance(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await getGuidanceHandler.HandleAsync(User.GetUserId(), submissionId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("guidance/{guidanceResponseId:guid}/follow-up")]
    public async Task<IActionResult> SubmitFollowUp(Guid guidanceResponseId, SubmitFollowUpRequest request, CancellationToken cancellationToken)
    {
        await submitFollowUpHandler.HandleAsync(new SubmitFollowUpCommand(User.GetUserId(), guidanceResponseId, request.Question), cancellationToken);
        return NoContent();
    }

    [HttpGet("export")]
    public async Task<ActionResult<QuestionnaireDataExportDto>> ExportMyData(CancellationToken cancellationToken)
    {
        var result = await exportMyQuestionnaireDataHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }
}
