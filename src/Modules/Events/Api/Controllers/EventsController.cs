using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Application.UseCases.Client;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Events.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
[Authorize(Policy = WellKnownPermissionKeys.EventsView)]
public sealed class EventsController(
    ListPublishedEventsHandler listPublishedEventsHandler,
    GetPublishedEventDetailHandler getPublishedEventDetailHandler,
    RegisterForEventHandler registerForEventHandler,
    CancelRegistrationHandler cancelRegistrationHandler,
    GetMyUpcomingEventHandler getMyUpcomingEventHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventSummaryDto>>> ListEvents(
        [FromQuery] bool includePast,
        [FromQuery] string language = "ro",
        CancellationToken cancellationToken = default)
    {
        var result = await listPublishedEventsHandler.HandleAsync(new ListPublishedEventsQuery(User.GetUserId(), language, includePast), cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-upcoming")]
    public async Task<ActionResult<MyRegistrationDto?>> GetMyUpcoming([FromQuery] string language = "ro", CancellationToken cancellationToken = default)
    {
        var result = await getMyUpcomingEventHandler.HandleAsync(User.GetUserId(), language, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(Guid eventId, [FromQuery] string language = "ro", CancellationToken cancellationToken = default)
    {
        var result = await getPublishedEventDetailHandler.HandleAsync(eventId, User.GetUserId(), language, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{eventId:guid}/register")]
    public async Task<IActionResult> Register(Guid eventId, CancellationToken cancellationToken)
    {
        var status = await registerForEventHandler.HandleAsync(eventId, User.GetUserId(), User.GetEmail(), cancellationToken);
        return Ok(new { status = status.ToString() });
    }

    [HttpPost("{eventId:guid}/cancel-registration")]
    public async Task<IActionResult> CancelRegistration(Guid eventId, CancellationToken cancellationToken)
    {
        await cancelRegistrationHandler.HandleAsync(eventId, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
