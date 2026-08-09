using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Application.UseCases.Admin;
using BUnited.Modules.Chat.Application.UseCases.Moderation;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Chat.Api.Controllers;

[ApiController]
[Route("api/v1/admin/chat")]
[Authorize(Policy = WellKnownPermissionKeys.ChatModerate)]
public sealed class AdminChatController(
    ListReportsHandler listReportsHandler,
    ResolveReportHandler resolveReportHandler,
    ListMutedUsersHandler listMutedUsersHandler,
    ListRecentModeratorActionsHandler listRecentModeratorActionsHandler,
    PinMessageHandler pinMessageHandler,
    DeleteMessageHandler deleteMessageHandler,
    MuteUserHandler muteUserHandler,
    CreateChatRoomHandler createChatRoomHandler,
    UpdateChatRoomHandler updateChatRoomHandler,
    ListChatRoomsAdminHandler listChatRoomsAdminHandler) : ControllerBase
{
    [HttpGet("rooms")]
    public async Task<ActionResult<IReadOnlyList<ChatRoomAdminDto>>> ListRooms(CancellationToken cancellationToken)
    {
        var result = await listChatRoomsAdminHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("rooms")]
    public async Task<ActionResult<Guid>> CreateRoom(CreateChatRoomRequest request, CancellationToken cancellationToken)
    {
        var roomId = await createChatRoomHandler.HandleAsync(
            new CreateChatRoomCommand(request.ProgramId, request.Key, request.Name, User.GetUserId()),
            cancellationToken);
        return CreatedAtAction(nameof(ListRooms), new { }, roomId);
    }

    [HttpPut("rooms/{roomId:guid}")]
    public async Task<IActionResult> UpdateRoom(Guid roomId, UpdateChatRoomRequest request, CancellationToken cancellationToken)
    {
        await updateChatRoomHandler.HandleAsync(new UpdateChatRoomCommand(roomId, request.Name, request.IsActive, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("reports")]
    public async Task<ActionResult<IReadOnlyList<ReportSummaryDto>>> ListReports([FromQuery] ReportStatus? status, CancellationToken cancellationToken)
    {
        var result = await listReportsHandler.HandleAsync(status, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reports/{reportId:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(Guid reportId, ResolveReportRequest request, CancellationToken cancellationToken)
    {
        var command = new ResolveReportCommand(reportId, User.GetUserId(), request.Action, request.MuteDurationMinutes, request.MuteReason);
        await resolveReportHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("muted-users")]
    public async Task<ActionResult<IReadOnlyList<MutedUserSummaryDto>>> ListMutedUsers(CancellationToken cancellationToken)
    {
        var result = await listMutedUsersHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent-actions")]
    public async Task<ActionResult<IReadOnlyList<ModeratorActionDto>>> ListRecentActions(CancellationToken cancellationToken)
    {
        var result = await listRecentModeratorActionsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("messages/{messageId:guid}/pin")]
    public async Task<IActionResult> PinMessage(Guid messageId, CancellationToken cancellationToken)
    {
        await pinMessageHandler.SetPinnedAsync(messageId, true, cancellationToken);
        return NoContent();
    }

    [HttpPost("messages/{messageId:guid}/unpin")]
    public async Task<IActionResult> UnpinMessage(Guid messageId, CancellationToken cancellationToken)
    {
        await pinMessageHandler.SetPinnedAsync(messageId, false, cancellationToken);
        return NoContent();
    }

    [HttpPost("messages/{messageId:guid}/delete")]
    public async Task<IActionResult> DeleteMessage(Guid messageId, CancellationToken cancellationToken)
    {
        await deleteMessageHandler.HandleAsync(messageId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/mute")]
    public async Task<IActionResult> MuteUser(Guid userId, MuteUserRequest request, CancellationToken cancellationToken)
    {
        await muteUserHandler.HandleAsync(new MuteUserCommand(userId, User.GetUserId(), request.DurationMinutes, request.Reason), cancellationToken);
        return NoContent();
    }
}
