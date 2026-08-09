using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>§53 — per-report actions: Dismiss, Delete Message, Mute User. Delete/Mute perform
/// their real side effect (reusing <see cref="DeleteMessageHandler"/>/<see cref="MuteUserHandler"/>
/// so the audit trail is identical to acting on a message/user directly) and resolve the report
/// as `Resolved`; a plain Dismiss resolves it as `Dismissed` with no side effect.</summary>
public sealed class ResolveReportHandler(DbContext dbContext, DeleteMessageHandler deleteMessageHandler, MuteUserHandler muteUserHandler)
{
    public async Task HandleAsync(ResolveReportCommand command, CancellationToken cancellationToken)
    {
        var report = await dbContext.Set<Report>().FirstOrDefaultAsync(r => r.Id == command.ReportId, cancellationToken)
            ?? throw new NotFoundAppException("The specified report does not exist.");

        var utcNow = DateTime.UtcNow;

        switch (command.Action)
        {
            case ReportResolutionAction.Dismiss:
                report.Resolve(ReportStatus.Dismissed, command.ModeratorId, utcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                break;

            case ReportResolutionAction.DeleteMessage:
                await deleteMessageHandler.HandleAsync(report.MessageId, command.ModeratorId, cancellationToken);
                report.Resolve(ReportStatus.Resolved, command.ModeratorId, utcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                break;

            case ReportResolutionAction.MuteUser:
                var message = await dbContext.Set<Message>().AsNoTracking().FirstOrDefaultAsync(m => m.Id == report.MessageId, cancellationToken)
                    ?? throw new NotFoundAppException("The reported message no longer exists.");
                await muteUserHandler.HandleAsync(
                    new MuteUserCommand(message.UserId, command.ModeratorId, command.MuteDurationMinutes, command.MuteReason),
                    cancellationToken);
                report.Resolve(ReportStatus.Resolved, command.ModeratorId, utcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                break;
        }
    }
}
