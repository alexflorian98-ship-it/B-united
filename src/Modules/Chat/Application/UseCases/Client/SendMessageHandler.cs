using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

/// <summary>P6.09: a currently-muted user cannot post — enforced server-side, not just hidden in
/// the UI. docs/TASKS.md P3.43.a: the room's owning program is resolved server-side and gated
/// with <see cref="IProgramAccessContext.RequireProgramAccessAsync"/> before the message is
/// persisted.</summary>
public sealed class SendMessageHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<MessageDto> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var room = await dbContext.Set<ChatRoom>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == command.RoomId, cancellationToken)
            ?? throw new NotFoundAppException("The specified room does not exist.");

        if (room.ProgramId is not null)
        {
            await programAccessContext.RequireProgramAccessAsync(command.UserId, room.ProgramId.Value, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;

        var isMuted = await dbContext.Set<Mute>().AsNoTracking()
            .AnyAsync(m => m.UserId == command.UserId && m.ExpiresAtUtc > utcNow, cancellationToken);

        if (isMuted)
        {
            throw new BusinessRuleAppException("CHAT_USER_MUTED", "errors.chat.userMuted", "You are temporarily muted and cannot post messages.");
        }

        var message = Message.Create(command.RoomId, command.UserId, command.Body);
        dbContext.Set<Message>().Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MessageDto(message.Id, message.UserId, null, message.Body, message.IsPinned, message.IsDeleted, message.CreatedAt);
    }
}
