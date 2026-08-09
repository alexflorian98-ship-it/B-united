using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.DataRights;

/// <summary>Exports the caller's own authored message history, capped at the most recent 1,000
/// messages to bound response size/memory (docs/DATA_RETENTION_POLICY.md).</summary>
public sealed class ChatUserDataExporter(DbContext dbContext) : IUserDataExporter
{
    private const int MaxMessages = 1000;

    public string SectionKey => "chat";

    public async Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<Message>().AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(MaxMessages)
            .Select(m => new
            {
                m.Id,
                m.RoomId,
                Body = m.IsDeleted ? null : m.Body,
                m.IsDeleted,
                m.CreatedAt,
            })
            .ToListAsync(cancellationToken);
}

/// <summary>docs/DATA_RETENTION_POLICY.md, "Chat — messages" / "moderation/read state": message
/// rows are deliberately NOT touched here — <see cref="Message.UserId"/> is an opaque reference
/// with no foreign key to <c>User</c>, and once the account is anonymized
/// (<c>DeleteMyAccountHandler</c>/<c>User.AnonymizeForDeletion</c>) the existing
/// <c>GetMessagesHandler</c> author-resolution path already renders it with no live user to
/// resolve to, satisfying "do not destroy conversation continuity... replace a deleted user's
/// identity with an anonymized representation" (docs/PROMPT.md §66) without any Chat-specific
/// code change. Only the user's own moderation/read-state bookkeeping is erased.</summary>
public sealed class ChatUserDataEraser(DbContext dbContext) : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var mutes = await dbContext.Set<Mute>().Where(m => m.UserId == userId).ToListAsync(cancellationToken);
        dbContext.Set<Mute>().RemoveRange(mutes);

        var reports = await dbContext.Set<Report>().Where(r => r.ReporterId == userId).ToListAsync(cancellationToken);
        dbContext.Set<Report>().RemoveRange(reports);

        var readStates = await dbContext.Set<ChatReadState>().Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        dbContext.Set<ChatReadState>().RemoveRange(readStates);
    }
}
