using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>P6.08: behind `chat.moderate`, soft delete + `chat.message_moderated` audit event.</summary>
public sealed class DeleteMessageHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task HandleAsync(Guid messageId, Guid moderatorId, CancellationToken cancellationToken)
    {
        var message = await dbContext.Set<Message>().FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            ?? throw new NotFoundAppException("The specified message does not exist.");

        message.SoftDelete(moderatorId, DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.ChatMessageModerated, moderatorId, nameof(Message), messageId.ToString()),
            cancellationToken);
    }
}
