using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class UpsertEventTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertEventTranslationCommand command, CancellationToken cancellationToken)
    {
        var translation = await dbContext.Set<EventTranslation>()
            .FirstOrDefaultAsync(t => t.EventId == command.EventId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            dbContext.Set<EventTranslation>().Add(EventTranslation.Create(command.EventId, command.Language, command.Title, command.Description));
        }
        else
        {
            translation.Update(command.Title, command.Description);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
