namespace BUnited.Modules.Chat.Application.UseCases.Admin;

public sealed record CreateChatRoomRequest(Guid ProgramId, string Key, string Name);

public sealed record CreateChatRoomCommand(Guid ProgramId, string Key, string Name, Guid CreatedBy);
