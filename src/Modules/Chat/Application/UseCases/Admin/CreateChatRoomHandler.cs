using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Content.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Admin;

/// <summary>docs/TASKS.md P3.43.a — admin-only room creation (clients cannot create rooms).
/// <see cref="CreateChatRoomCommand.ProgramId"/> is validated against Content via
/// <see cref="IProgramLookup"/> (a Contracts-only cross-module read, never Content's
/// Domain/Infrastructure — CLAUDE.md) and must reference a published program, mirroring
/// <c>CreateProgramOfferHandler</c>/<c>CreateQuestionnaireHandler</c> for consistency.</summary>
public sealed class CreateChatRoomHandler(DbContext dbContext, IProgramLookup programLookup)
{
    public async Task<Guid> HandleAsync(CreateChatRoomCommand command, CancellationToken cancellationToken)
    {
        var program = await programLookup.GetProgramAsync(command.ProgramId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist.");

        if (program.Status != ProgramLookupStatus.Published)
        {
            throw new BusinessRuleAppException(
                "CHAT_ROOM_PROGRAM_NOT_PUBLISHED",
                "errors.chat.programNotPublished",
                "A room can only be created for a published program.");
        }

        var keyTaken = await dbContext.Set<ChatRoom>().AnyAsync(r => r.Key == command.Key, cancellationToken);
        if (keyTaken)
        {
            throw new BusinessRuleAppException("CHAT_ROOM_KEY_TAKEN", "errors.chat.roomKeyTaken", "A room with this key already exists.");
        }

        var room = ChatRoom.Create(command.ProgramId, command.Key, command.Name, command.CreatedBy);
        dbContext.Set<ChatRoom>().Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);

        return room.Id;
    }
}
