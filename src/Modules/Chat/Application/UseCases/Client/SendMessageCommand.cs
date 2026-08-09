namespace BUnited.Modules.Chat.Application.UseCases.Client;

public sealed record SendMessageRequest(string Body);

public sealed record SendMessageCommand(Guid RoomId, Guid UserId, string Body);
