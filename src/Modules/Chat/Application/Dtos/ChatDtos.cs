namespace BUnited.Modules.Chat.Application.Dtos;

/// <summary>Rooms are listed openly to every <c>chat.use</c> caller (browsable catalogue metadata,
/// not the protected resource itself), mirroring Questionnaires' <c>ListPublishedQuestionnairesHandler</c>
/// — <see cref="HasAccess"/> tells the client whether the room is actually joinable so it can show
/// a locked state instead of hiding the room entirely; the real gate is still enforced server-side
/// on history/post/read routes regardless of what this flag says.</summary>
public sealed record RoomSummaryDto(Guid RoomId, string Key, string Name, Guid? ProgramId, bool HasAccess, string? LastMessagePreview, DateTime? LastMessageAtUtc, int UnreadCount);

public sealed record ChatRoomAdminDto(Guid Id, Guid? ProgramId, string Key, string Name, bool IsActive, DateTime CreatedAt);

public sealed record MessageDto(Guid Id, Guid UserId, string? Email, string? Body, bool IsPinned, bool IsDeleted, DateTime CreatedAt);

public sealed record MessagePageResult(IReadOnlyList<MessageDto> Items, DateTime? NextBeforeCursor);

public sealed record ReportSummaryDto(
    Guid ReportId,
    Guid MessageId,
    string? MessageBody,
    Guid MessageAuthorUserId,
    string? MessageAuthorEmail,
    Guid ReporterUserId,
    string? ReporterEmail,
    string Reason,
    string Status,
    DateTime CreatedAt);

public sealed record MutedUserSummaryDto(Guid MuteId, Guid UserId, string? Email, string? Reason, DateTime ExpiresAtUtc, string? ModeratorEmail);

public sealed record ModeratorActionDto(string Kind, string? ActorEmail, string TargetDescription, DateTime OccurredAtUtc);
