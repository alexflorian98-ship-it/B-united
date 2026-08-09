using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Application.UseCases.DataRights;

public sealed class EventsUserDataExporter(DbContext dbContext) : IUserDataExporter
{
    public string SectionKey => "events";

    public async Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<EventRegistration>().AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.EventId,
                Status = r.Status.ToString(),
                r.CreatedAt,
                r.CanceledAt,
            })
            .ToListAsync(cancellationToken);
}

/// <summary>docs/DATA_RETENTION_POLICY.md, "Events — soft cancel instead of hard delete": reuses
/// <see cref="EventRegistration.Cancel"/> rather than deleting rows, so capacity/attendance
/// history stays internally consistent. Does not itself trigger waitlist promotion for the next
/// person in line — see the policy doc's documented residual risk.</summary>
public sealed class EventsUserDataEraser(DbContext dbContext, TimeProvider timeProvider) : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var registrations = await dbContext.Set<EventRegistration>()
            .Where(r => r.UserId == userId && r.Status != EventRegistrationStatus.Canceled)
            .ToListAsync(cancellationToken);

        foreach (var registration in registrations)
        {
            registration.Cancel(utcNow);
        }
    }
}
