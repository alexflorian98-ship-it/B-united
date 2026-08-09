using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>P6.09: behind `chat.moderate`, `chat.user_muted` audit event.</summary>
public sealed class MuteUserHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task<Guid> HandleAsync(MuteUserCommand command, CancellationToken cancellationToken)
    {
        var mute = Mute.Create(command.UserId, command.ModeratorId, command.Reason, DateTime.UtcNow.AddMinutes(command.DurationMinutes));
        dbContext.Set<Mute>().Add(mute);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.ChatUserMuted, command.ModeratorId, nameof(Mute), mute.Id.ToString()),
            cancellationToken);

        return mute.Id;
    }
}
