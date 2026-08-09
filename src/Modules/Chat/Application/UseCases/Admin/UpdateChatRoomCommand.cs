namespace BUnited.Modules.Chat.Application.UseCases.Admin;

public sealed record UpdateChatRoomRequest(string Name, bool IsActive);

public sealed record UpdateChatRoomCommand(Guid RoomId, string Name, bool IsActive, Guid UpdatedBy);
