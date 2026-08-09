using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;

namespace BUnited.Modules.Events.Tests.Domain;

public sealed class EventTests
{
    private static Event CreateEvent(DateTime startsAtUtc, DateTime endsAtUtc) =>
        Event.Create("ro", startsAtUtc, endsAtUtc, "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example.com/x", null, null);

    [Fact]
    public void EndsAtUtc_before_StartsAtUtc_is_rejected()
    {
        var startsAtUtc = DateTime.UtcNow.AddDays(1);
        Assert.Throws<InvalidOperationException>(() => CreateEvent(startsAtUtc, startsAtUtc.AddHours(-1)));
    }

    [Fact]
    public void EffectiveStatus_is_Completed_once_a_published_event_has_ended_even_though_stored_status_stays_Published()
    {
        var @event = CreateEvent(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(1));
        @event.Publish(DateTime.UtcNow.AddDays(-2), Guid.NewGuid());

        Assert.Equal(EventStatus.Published, @event.Status);
        Assert.Equal(EventStatus.Completed, @event.EffectiveStatus(DateTime.UtcNow));
    }

    [Fact]
    public void EffectiveStatus_stays_Published_before_the_event_ends()
    {
        var startsAtUtc = DateTime.UtcNow.AddDays(1);
        var @event = CreateEvent(startsAtUtc, startsAtUtc.AddHours(1));
        @event.Publish(DateTime.UtcNow, Guid.NewGuid());

        Assert.Equal(EventStatus.Published, @event.EffectiveStatus(DateTime.UtcNow));
    }

    [Fact]
    public void A_canceled_event_cannot_be_published()
    {
        var startsAtUtc = DateTime.UtcNow.AddDays(1);
        var @event = CreateEvent(startsAtUtc, startsAtUtc.AddHours(1));
        @event.Cancel(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => @event.Publish(DateTime.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Storage_is_always_UTC_while_DisplayTimezone_is_kept_separately()
    {
        var startsAtUtc = new DateTime(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc);
        var @event = CreateEvent(startsAtUtc, startsAtUtc.AddHours(1));

        // The entity never converts StartsAtUtc using DisplayTimezone — that conversion is a
        // presentation concern (frontend), matching docs/DEVELOPMENT_INSTRUCTIONS.md §5's
        // "Persist timestamps in UTC. Event records MUST retain their display timezone separately."
        Assert.Equal(startsAtUtc, @event.StartsAtUtc);
        Assert.Equal("Europe/Bucharest", @event.DisplayTimezone);
    }
}
