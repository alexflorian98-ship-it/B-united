using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>P6.07: behind `chat.moderate`.</summary>
public sealed class PinMessageHandler(DbContext dbContext)
{
    public async Task SetPinnedAsync(Guid messageId, bool pinned, CancellationToken cancellationToken)
    {
        var message = await dbContext.Set<Message>().FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            ?? throw new NotFoundAppException("The specified message does not exist.");

        if (pinned)
        {
            message.Pin();
        }
        else
        {
            message.Unpin();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
