using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

/// <summary>docs/TASKS.md P3.43.a: reporting is only meaningful for a message the caller could
/// otherwise read, so it is gated the same way as history/post — the reported message's room's
/// owning program is resolved server-side and checked with
/// <see cref="IProgramAccessContext.RequireProgramAccessAsync"/>.</summary>
public sealed class ReportMessageHandler(DbContext dbContext, IProgramAccessContext programAccessContext)
{
    public async Task<Guid> HandleAsync(ReportMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await dbContext.Set<Message>().AsNoTracking().FirstOrDefaultAsync(m => m.Id == command.MessageId, cancellationToken)
            ?? throw new NotFoundAppException("The specified message does not exist.");

        var room = await dbContext.Set<ChatRoom>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == message.RoomId, cancellationToken);
        if (room?.ProgramId is not null)
        {
            await programAccessContext.RequireProgramAccessAsync(command.ReporterId, room.ProgramId.Value, cancellationToken);
        }

        var report = Report.Create(command.MessageId, command.ReporterId, command.Reason);
        dbContext.Set<Report>().Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        return report.Id;
    }
}
