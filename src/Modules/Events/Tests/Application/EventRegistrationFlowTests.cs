using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Application;
using BUnited.Modules.Events.Application.UseCases.Client;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Tests.TestSupport;
using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Events.Tests.Application;

public sealed class EventRegistrationFlowTests
{
    private sealed record Fixture(
        TestDbContext DbContext,
        FakeNotificationSender NotificationSender,
        FakeProgramAccessContext ProgramAccessContext,
        RegisterForEventHandler RegisterHandler,
        CancelRegistrationHandler CancelHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var notificationSender = new FakeNotificationSender();
        var programAccessContext = new FakeProgramAccessContext();
        var registerHandler = new RegisterForEventHandler(context, notificationSender, programAccessContext);
        var cancelHandler = new CancelRegistrationHandler(context);
        return new Fixture(context, notificationSender, programAccessContext, registerHandler, cancelHandler);
    }

    private static Event SeedEvent(TestDbContext dbContext, DateTime startsAtUtc, int? capacity)
    {
        var @event = Event.Create("ro", startsAtUtc, startsAtUtc.AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example.com/x", capacity, null);
        dbContext.Events.Add(@event);
        dbContext.EventTranslations.Add(EventTranslation.Create(@event.Id, "ro", "Live Q&A", "Session description"));
        @event.Publish(DateTime.UtcNow, Guid.NewGuid());
        dbContext.SaveChanges();
        return @event;
    }

    [Fact]
    public async Task Registration_closes_once_the_event_has_started()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var userId = Guid.NewGuid();
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddMinutes(-1), capacity: null);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None));

        Assert.Equal("EVENT_REGISTRATION_CLOSED", ex.Code);
    }

    [Fact]
    public async Task Registration_beyond_capacity_is_waitlisted()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: 1);

        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        var firstStatus = await fx.RegisterHandler.HandleAsync(@event.Id, firstUser, "first@example.com", CancellationToken.None);
        var secondStatus = await fx.RegisterHandler.HandleAsync(@event.Id, secondUser, "second@example.com", CancellationToken.None);

        Assert.Equal(EventRegistrationStatus.Registered, firstStatus);
        Assert.Equal(EventRegistrationStatus.Waitlisted, secondStatus);
        Assert.Equal(2, fx.NotificationSender.Sent.Count);
    }

    [Fact]
    public async Task Canceling_a_registered_seat_promotes_the_oldest_waitlisted_user()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: 1);

        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var thirdUser = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, firstUser, "first@example.com", CancellationToken.None);
        await fx.RegisterHandler.HandleAsync(@event.Id, secondUser, "second@example.com", CancellationToken.None);
        await fx.RegisterHandler.HandleAsync(@event.Id, thirdUser, "third@example.com", CancellationToken.None);

        await fx.CancelHandler.HandleAsync(@event.Id, firstUser, CancellationToken.None);

        var second = fx.DbContext.EventRegistrations.Single(r => r.UserId == secondUser);
        var third = fx.DbContext.EventRegistrations.Single(r => r.UserId == thirdUser);
        Assert.Equal(EventRegistrationStatus.Registered, second.Status);
        Assert.Equal(EventRegistrationStatus.Waitlisted, third.Status);

        // The promoted registration now gets reminder rows scheduled — it didn't have any while waitlisted.
        Assert.Contains(fx.DbContext.EventReminders, r => r.EventRegistrationId == second.Id);
    }

    [Fact]
    public async Task Canceling_a_waitlisted_registration_does_not_promote_anyone()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: 1);

        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, firstUser, "first@example.com", CancellationToken.None);
        await fx.RegisterHandler.HandleAsync(@event.Id, secondUser, "second@example.com", CancellationToken.None);

        await fx.CancelHandler.HandleAsync(@event.Id, secondUser, CancellationToken.None);

        var first = fx.DbContext.EventRegistrations.Single(r => r.UserId == firstUser);
        var second = fx.DbContext.EventRegistrations.Single(r => r.UserId == secondUser);
        Assert.Equal(EventRegistrationStatus.Registered, first.Status);
        Assert.Equal(EventRegistrationStatus.Canceled, second.Status);
    }

    [Fact]
    public async Task Re_registering_after_cancellation_reactivates_the_same_row()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: null);
        var userId = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);
        await fx.CancelHandler.HandleAsync(@event.Id, userId, CancellationToken.None);
        await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);

        Assert.Single(fx.DbContext.EventRegistrations.Where(r => r.EventId == @event.Id && r.UserId == userId));
        var registration = fx.DbContext.EventRegistrations.Single(r => r.EventId == @event.Id && r.UserId == userId);
        Assert.Equal(EventRegistrationStatus.Registered, registration.Status);
    }

    [Fact]
    public async Task Double_registration_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: null);
        var userId = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None));

        Assert.Equal("EVENT_ALREADY_REGISTERED", ex.Code);
    }

    [Fact]
    public async Task Registering_close_to_the_event_skips_reminders_whose_lead_time_has_already_passed()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        // Only 30 minutes until start — neither the 24h nor the 1h lead time is still in the future.
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddMinutes(30), capacity: null);
        var userId = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);

        Assert.Empty(fx.DbContext.EventReminders);
    }

    [Fact]
    public async Task Registering_well_ahead_schedules_both_reminders()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(5), capacity: null);
        var userId = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);

        var reminders = fx.DbContext.EventReminders.Where(r => r.EventId == @event.Id).ToList();
        Assert.Equal(2, reminders.Count);
        Assert.Contains(reminders, r => r.Type == EventReminderType.TwentyFourHour);
        Assert.Contains(reminders, r => r.Type == EventReminderType.OneHour);
    }

    [Fact]
    public async Task Registration_on_an_event_with_zero_associated_programs_stays_open_to_any_authenticated_user()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: null);
        var userId = Guid.NewGuid();

        var status = await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);

        Assert.Equal(EventRegistrationStatus.Registered, status);
    }

    [Fact]
    public async Task Registration_on_a_program_restricted_event_is_denied_without_access_to_any_associated_program()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: null);
        var programA = Guid.NewGuid();
        var programB = Guid.NewGuid();
        fx.DbContext.Set<EventProgram>().Add(EventProgram.Create(@event.Id, programA));
        fx.DbContext.Set<EventProgram>().Add(EventProgram.Create(@event.Id, programB));
        fx.DbContext.SaveChanges();
        var userId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None));

        Assert.Equal("PROGRAM_ACCESS_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Registration_on_a_program_restricted_event_succeeds_with_access_to_at_least_one_associated_program()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: null);
        var programA = Guid.NewGuid();
        var programB = Guid.NewGuid();
        fx.DbContext.Set<EventProgram>().Add(EventProgram.Create(@event.Id, programA));
        fx.DbContext.Set<EventProgram>().Add(EventProgram.Create(@event.Id, programB));
        fx.DbContext.SaveChanges();
        var userId = Guid.NewGuid();
        fx.ProgramAccessContext.GrantAccess(userId, programB);

        var status = await fx.RegisterHandler.HandleAsync(@event.Id, userId, "client@example.com", CancellationToken.None);

        Assert.Equal(EventRegistrationStatus.Registered, status);
    }
}
