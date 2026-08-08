using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Progress.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Progress.Api.Controllers;

[ApiController]
[Route("api/v1/progress")]
[Authorize(Policy = WellKnownPermissionKeys.ContentView)]
public sealed class ProgressController(
    RecordVideoProgressHandler recordVideoProgressHandler,
    MarkContentCompletedHandler markContentCompletedHandler,
    GetContentProgressHandler getContentProgressHandler,
    GetSectionProgressHandler getSectionProgressHandler) : ControllerBase
{
    [HttpPost("video-position")]
    public async Task<IActionResult> RecordVideoPosition(RecordVideoProgressRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordVideoProgressCommand(
            User.GetUserId(), request.ContentItemId, request.SectionId, request.SectionContentItemIds, request.PositionSeconds, request.WatchPercentage);
        await recordVideoProgressHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("complete")]
    public async Task<IActionResult> MarkCompleted(MarkContentCompletedRequest request, CancellationToken cancellationToken)
    {
        var command = new MarkContentCompletedCommand(User.GetUserId(), request.ContentItemId, request.SectionId, request.SectionContentItemIds);
        await markContentCompletedHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("content-items")]
    public async Task<ActionResult<IReadOnlyList<ContentProgressDto>>> GetContentProgress([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        var result = await getContentProgressHandler.HandleAsync(User.GetUserId(), ids, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sections")]
    public async Task<ActionResult<IReadOnlyList<SectionProgressDto>>> GetSectionProgress([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        var result = await getSectionProgressHandler.HandleAsync(User.GetUserId(), ids, cancellationToken);
        return Ok(result);
    }
}
