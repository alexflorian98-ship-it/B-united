namespace BUnited.Modules.Chat.Contracts;

/// <summary>Cross-module contract consumed by Content's program-publish flow (mirrors the
/// existing direction of <c>Chat.Application</c> consuming <c>Content.Contracts.IProgramLookup</c>
/// — CLAUDE.md: cross-module dependencies go through Contracts only). Guarantees every published
/// program has exactly one chat room, named after the program itself, without requiring a
/// separate manual admin step. Access to the room is never granted here: <c>ListRoomsHandler</c>
/// already computes <c>HasAccess</c> dynamically from the caller's real program entitlement, so a
/// client is "added" to the room automatically the moment they own the program — the same way
/// every other program-gated resource works in this codebase.</summary>
public interface IProgramChatRoomProvisioner
{
    /// <summary>Idempotent: a program that already has a room (active or not — an admin's
    /// deliberate deactivation is never silently undone) is left untouched.</summary>
    Task EnsureRoomForProgramAsync(Guid programId, string programName, Guid? createdBy, CancellationToken cancellationToken);
}
