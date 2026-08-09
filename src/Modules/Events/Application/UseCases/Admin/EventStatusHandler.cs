using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class EventStatusHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task PublishAsync(Guid eventId, Guid actorId, CancellationToken cancellationToken)
    {
        var @event = await GetEventAsync(eventId, cancellationToken);
        @event.Publish(DateTime.UtcNow, actorId);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.EventPublished, actorId, nameof(Event), eventId.ToString()),
            cancellationToken);
    }

    public async Task CancelAsync(Guid eventId, Guid actorId, CancellationToken cancellationToken)
    {
        var @event = await GetEventAsync(eventId, cancellationToken);
        @event.Cancel(actorId);

        var utcNow = DateTime.UtcNow;
        var activeRegistrations = await dbContext.Set<EventRegistration>()
            .Where(r => r.EventId == eventId && r.Status != EventRegistrationStatus.Canceled)
            .ToListAsync(cancellationToken);

        foreach (var registration in activeRegistrations)
        {
            registration.Cancel(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.EventCanceled, actorId, nameof(Event), eventId.ToString()),
            cancellationToken);
    }

    private async Task<Event> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.Set<Event>().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new NotFoundAppException("The specified event does not exist.");
}
