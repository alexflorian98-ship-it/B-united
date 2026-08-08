using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Application.UseCases.Expert;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Questionnaires.Api.Controllers;

/// <summary>Expert review/guidance workflow (§25–28/§50). Queue and submission reads require
/// <c>questionnaire.review</c>; authoring/publishing guidance requires the stronger
/// <c>questionnaire.answer</c> — a reviewer is not automatically able to write clinical-adjacent
/// guidance text.</summary>
[ApiController]
[Route("api/v1/expert/questionnaires")]
[Authorize]
public sealed class ExpertQuestionnairesController(
    GetQueueHandler getQueueHandler,
    GetSubmissionDetailForExpertHandler getSubmissionDetailForExpertHandler,
    SaveGuidanceDraftHandler saveGuidanceDraftHandler,
    PublishGuidanceHandler publishGuidanceHandler,
    AnswerFollowUpHandler answerFollowUpHandler) : ControllerBase
{
    [HttpGet("queue")]
    [Authorize(Policy = WellKnownPermissionKeys.QuestionnaireReview)]
    public async Task<ActionResult<IReadOnlyList<QueueItemDto>>> GetQueue(CancellationToken cancellationToken)
    {
        var result = await getQueueHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("submissions/{submissionId:guid}")]
    [Authorize(Policy = WellKnownPermissionKeys.QuestionnaireReview)]
    public async Task<ActionResult<SubmissionDetailForExpertDto>> GetSubmission(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await getSubmissionDetailForExpertHandler.HandleAsync(submissionId, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("submissions/{submissionId:guid}/guidance")]
    [Authorize(Policy = WellKnownPermissionKeys.QuestionnaireAnswer)]
    public async Task<ActionResult<Guid>> SaveGuidanceDraft(Guid submissionId, SaveGuidanceDraftRequest request, CancellationToken cancellationToken)
    {
        var id = await saveGuidanceDraftHandler.HandleAsync(new SaveGuidanceDraftCommand(submissionId, request.Body, User.GetUserId()), cancellationToken);
        return Ok(id);
    }

    [HttpPost("guidance/{guidanceResponseId:guid}/publish")]
    [Authorize(Policy = WellKnownPermissionKeys.QuestionnaireAnswer)]
    public async Task<IActionResult> PublishGuidance(Guid guidanceResponseId, CancellationToken cancellationToken)
    {
        await publishGuidanceHandler.HandleAsync(guidanceResponseId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("follow-up/{followUpId:guid}/answer")]
    [Authorize(Policy = WellKnownPermissionKeys.QuestionnaireAnswer)]
    public async Task<IActionResult> AnswerFollowUp(Guid followUpId, AnswerFollowUpRequest request, CancellationToken cancellationToken)
    {
        await answerFollowUpHandler.HandleAsync(new AnswerFollowUpCommand(followUpId, request.Answer), cancellationToken);
        return NoContent();
    }
}
