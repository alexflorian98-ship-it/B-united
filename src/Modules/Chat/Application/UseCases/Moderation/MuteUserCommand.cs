namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

public sealed record MuteUserRequest(int DurationMinutes, string? Reason);

public sealed record MuteUserCommand(Guid UserId, Guid ModeratorId, int DurationMinutes, string? Reason);
