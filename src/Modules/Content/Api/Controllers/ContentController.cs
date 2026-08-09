using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Application.UseCases.Client;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Content.Api.Controllers;

/// <summary>Client-facing, published-only reads (P2.12) — see <c>AdminContentController</c> for
/// the authoring surface.</summary>
[ApiController]
[Route("api/v1/content")]
[Authorize(Policy = WellKnownPermissionKeys.ContentView)]
public sealed class ContentController(
    ListContentDomainsHandler listContentDomainsHandler,
    ListPublishedProgramsHandler listPublishedProgramsHandler,
    GetPublishedProgramDetailHandler getPublishedProgramDetailHandler,
    GetVideoPlaybackHandler getVideoPlaybackHandler,
    SubmitQuizAttemptHandler submitQuizAttemptHandler) : ControllerBase
{
    [HttpGet("domains")]
    public async Task<ActionResult<IReadOnlyList<ContentDomainDto>>> ListDomains(CancellationToken cancellationToken)
    {
        var result = await listContentDomainsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("programs")]
    public async Task<ActionResult<IReadOnlyList<ClientProgramSummaryDto>>> ListPrograms(
        [FromQuery] Guid? domainId,
        [FromQuery] string language = "ro",
        CancellationToken cancellationToken = default)
    {
        var result = await listPublishedProgramsHandler.HandleAsync(new ListPublishedProgramsQuery(domainId, language, User.GetUserId()), cancellationToken);
        return Ok(result);
    }

    [HttpGet("programs/{slug}")]
    public async Task<ActionResult<ClientProgramDetailDto>> GetProgram(string slug, [FromQuery] string language = "ro", CancellationToken cancellationToken = default)
    {
        var result = await getPublishedProgramDetailHandler.HandleAsync(slug, language, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("content-items/{contentItemId:guid}/playback")]
    public async Task<ActionResult<VideoPlaybackResult>> GetVideoPlayback(Guid contentItemId, CancellationToken cancellationToken)
    {
        var result = await getVideoPlaybackHandler.HandleAsync(contentItemId, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("content-items/{contentItemId:guid}/quiz/submit")]
    public async Task<ActionResult<SubmitQuizAttemptResult>> SubmitQuiz(Guid contentItemId, SubmitQuizRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitQuizAttemptCommand(
            contentItemId,
            User.GetUserId(),
            request.Answers.Select(a => new QuizAnswerInput(a.QuestionId, a.SelectedOptionId)).ToList());
        var result = await submitQuizAttemptHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }
}
