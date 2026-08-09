using BUnited.Modules.Events.Application.Dtos;
using BUnited.Modules.Events.Application.UseCases.Admin;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Events.Api.Controllers;

[ApiController]
[Route("api/v1/admin/events")]
[Authorize(Policy = WellKnownPermissionKeys.EventsManage)]
public sealed class AdminEventsController(
    CreateEventHandler createEventHandler,
    UpsertEventTranslationHandler upsertEventTranslationHandler,
    UpdateEventScheduleHandler updateEventScheduleHandler,
    EventStatusHandler eventStatusHandler,
    ListEventsHandler listEventsHandler,
    GetEventDetailAdminHandler getEventDetailAdminHandler,
    SetEventProgramsHandler setEventProgramsHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminEventListResult>> ListEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await listEventsHandler.HandleAsync(new ListEventsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<AdminEventDetailDto>> GetEvent(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await getEventDetailAdminHandler.HandleAsync(eventId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateEvent(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand(
            request.DefaultLanguage,
            request.Title,
            request.Description,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.DisplayTimezone,
            request.LocationType,
            request.Location,
            request.MeetingUrl,
            request.Capacity,
            User.GetUserId());

        var eventId = await createEventHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetEvent), new { eventId }, eventId);
    }

    [HttpPut("{eventId:guid}/translations")]
    public async Task<IActionResult> UpsertTranslation(Guid eventId, UpsertEventTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertEventTranslationHandler.HandleAsync(new UpsertEventTranslationCommand(eventId, request.Language, request.Title, request.Description), cancellationToken);
        return NoContent();
    }

    [HttpPut("{eventId:guid}/schedule")]
    public async Task<IActionResult> UpdateSchedule(Guid eventId, UpdateEventScheduleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateEventScheduleCommand(
            eventId,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.DisplayTimezone,
            request.LocationType,
            request.Location,
            request.MeetingUrl,
            request.Capacity,
            User.GetUserId());

        await updateEventScheduleHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{eventId:guid}/programs")]
    public async Task<IActionResult> SetPrograms(Guid eventId, SetEventProgramsRequest request, CancellationToken cancellationToken)
    {
        await setEventProgramsHandler.HandleAsync(new SetEventProgramsCommand(eventId, request.ProgramIds, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{eventId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid eventId, CancellationToken cancellationToken)
    {
        await eventStatusHandler.PublishAsync(eventId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{eventId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid eventId, CancellationToken cancellationToken)
    {
        await eventStatusHandler.CancelAsync(eventId, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
