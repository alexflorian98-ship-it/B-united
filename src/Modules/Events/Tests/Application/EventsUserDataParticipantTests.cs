using BUnited.Modules.Events.Application.UseCases.DataRights;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Events.Tests.Application;

/// <summary>docs/DATA_RETENTION_POLICY.md, "Events — soft cancel instead of hard delete".</summary>
public sealed class EventsUserDataParticipantTests
{
    [Fact]
    public async Task Erase_cancels_active_registrations_instead_of_deleting_them()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var userId = Guid.NewGuid();
        var eventOne = CreateEvent();
        var eventTwo = CreateEvent();
        context.Events.AddRange(eventOne, eventTwo);
        var registered = EventRegistration.Create(eventOne.Id, userId, EventRegistrationStatus.Registered);
        var waitlisted = EventRegistration.Create(eventTwo.Id, userId, EventRegistrationStatus.Waitlisted);
        context.EventRegistrations.AddRange(registered, waitlisted);
        await context.SaveChangesAsync();

        var eraser = new EventsUserDataEraser(context, TimeProvider.System);
        await eraser.EraseAsync(userId, CancellationToken.None);
        await context.SaveChangesAsync();

        var reloaded = await context.EventRegistrations.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();
        Assert.Equal(2, reloaded.Count);
        Assert.All(reloaded, r => Assert.Equal(EventRegistrationStatus.Canceled, r.Status));
        Assert.All(reloaded, r => Assert.NotNull(r.CanceledAt));
    }

    [Fact]
    public async Task Export_returns_only_the_callers_own_registrations()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var eventOne = CreateEvent();
        var eventTwo = CreateEvent();
        context.Events.AddRange(eventOne, eventTwo);
        context.EventRegistrations.Add(EventRegistration.Create(eventOne.Id, userId, EventRegistrationStatus.Registered));
        context.EventRegistrations.Add(EventRegistration.Create(eventTwo.Id, otherUserId, EventRegistrationStatus.Registered));
        await context.SaveChangesAsync();

        var exporter = new EventsUserDataExporter(context);
        var result = await exporter.ExportAsync(userId, CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.Contains(eventOne.Id.ToString(), json);
        Assert.DoesNotContain(otherUserId.ToString(), json);
    }

    private static Event CreateEvent() =>
        Event.Create(
            "ro",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(1),
            "Europe/Bucharest",
            EventLocationType.Online,
            location: null,
            meetingUrl: "https://example.com/meeting",
            capacity: 10,
            createdBy: null);
}
