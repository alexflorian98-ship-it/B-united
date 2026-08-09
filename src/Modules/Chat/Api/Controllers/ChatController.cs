using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Application.UseCases.Client;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Chat.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize(Policy = WellKnownPermissionKeys.ChatUse)]
public sealed class ChatController(
    ListRoomsHandler listRoomsHandler,
    GetMessagesHandler getMessagesHandler,
    SendMessageHandler sendMessageHandler,
    MarkRoomReadHandler markRoomReadHandler,
    ReportMessageHandler reportMessageHandler) : ControllerBase
{
    [HttpGet("rooms")]
    public async Task<ActionResult<IReadOnlyList<RoomSummaryDto>>> ListRooms(CancellationToken cancellationToken)
    {
        var result = await listRoomsHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("rooms/{roomId:guid}/messages")]
    public async Task<ActionResult<MessagePageResult>> GetMessages(Guid roomId, [FromQuery] DateTime? before, CancellationToken cancellationToken)
    {
        var result = await getMessagesHandler.HandleAsync(roomId, User.GetUserId(), before, cancellationToken);
        return Ok(result);
    }

    [HttpPost("rooms/{roomId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid roomId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await sendMessageHandler.HandleAsync(new SendMessageCommand(roomId, User.GetUserId(), request.Body), cancellationToken);
        return Ok(result);
    }

    [HttpPost("rooms/{roomId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid roomId, CancellationToken cancellationToken)
    {
        await markRoomReadHandler.HandleAsync(roomId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("messages/{messageId:guid}/report")]
    public async Task<IActionResult> ReportMessage(Guid messageId, ReportMessageRequest request, CancellationToken cancellationToken)
    {
        await reportMessageHandler.HandleAsync(new ReportMessageCommand(messageId, User.GetUserId(), request.Reason), cancellationToken);
        return NoContent();
    }
}
