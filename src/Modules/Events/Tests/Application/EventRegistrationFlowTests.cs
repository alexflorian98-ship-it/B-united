using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Events.Application;
using BUnited.Modules.Events.Application.UseCases.Client;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Tests.TestSupport;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Notifications.Contracts;

namespace BUnited.Modules.Events.Tests.Application;

public sealed class EventRegistrationFlowTests
{
    private sealed record Fixture(
        TestDbContext DbContext,
        FakeNotificationSender NotificationSender,
        FakeUserLookup UserLookup,
        FakeProgramAccessContext ProgramAccessContext,
        RegisterForEventHandler RegisterHandler,
        CancelRegistrationHandler CancelHandler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var notificationSender = new FakeNotificationSender();
        var userLookup = new FakeUserLookup();
        var programAccessContext = new FakeProgramAccessContext();
        var registerHandler = new RegisterForEventHandler(context, notificationSender, programAccessContext);
        var cancelHandler = new CancelRegistrationHandler(context, userLookup, notificationSender);
        return new Fixture(context, notificationSender, userLookup, programAccessContext, registerHandler, cancelHandler);
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
        fx.UserLookup.Users[secondUser] = new UserSummary(secondUser, "second@example.com", null);

        await fx.RegisterHandler.HandleAsync(@event.Id, firstUser, "first@example.com", CancellationToken.None);
        await fx.RegisterHandler.HandleAsync(@event.Id, secondUser, "second@example.com", CancellationToken.None);
        await fx.RegisterHandler.HandleAsync(@event.Id, thirdUser, "third@example.com", CancellationToken.None);
        fx.NotificationSender.Sent.Clear();

        await fx.CancelHandler.HandleAsync(@event.Id, firstUser, CancellationToken.None);

        var second = fx.DbContext.EventRegistrations.Single(r => r.UserId == secondUser);
        var third = fx.DbContext.EventRegistrations.Single(r => r.UserId == thirdUser);
        Assert.Equal(EventRegistrationStatus.Registered, second.Status);
        Assert.Equal(EventRegistrationStatus.Waitlisted, third.Status);

        // The promoted registration now gets reminder rows scheduled — it didn't have any while waitlisted.
        Assert.Contains(fx.DbContext.EventReminders, r => r.EventRegistrationId == second.Id);

        // P5.12.b: the promoted user is notified at promotion time, not left to discover it later.
        var sent = Assert.Single(fx.NotificationSender.Sent);
        Assert.Equal(NotificationType.EventRegistrationConfirmed, sent.Type);
        Assert.Equal("second@example.com", sent.RecipientEmail);
        Assert.Equal(EventRegistrationStatus.Registered.ToString(), sent.TemplateData["status"]);
    }

    [Fact]
    public async Task Canceling_a_registered_seat_with_no_waitlisted_users_sends_no_promotion_notification()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: 1);
        var onlyUser = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, onlyUser, "only@example.com", CancellationToken.None);
        fx.NotificationSender.Sent.Clear();

        await fx.CancelHandler.HandleAsync(@event.Id, onlyUser, CancellationToken.None);

        Assert.Empty(fx.NotificationSender.Sent);
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

    /// <summary>Security-gap-closure item #1 (two-user IDOR suite, "event registrations"):
    /// <see cref="CancelRegistrationHandler"/> is scoped by (eventId, callerUserId) taken from the
    /// JWT — there is no client-suppliable "registration id" to attack — so the only IDOR shape
    /// possible here is a caller with no registration of their own attempting to cancel on an
    /// event where a DIFFERENT user has a real seat. Proves that attempt fails closed (404, not a
    /// silent no-op that could be confused with success) and leaves the other user's registration
    /// completely untouched — no state change after a rejected mutation.</summary>
    [Fact]
    public async Task Canceling_with_no_registration_of_ones_own_fails_and_leaves_another_users_registration_untouched()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext, DateTime.UtcNow.AddDays(2), capacity: null);
        var registeredUser = Guid.NewGuid();
        var attackerUser = Guid.NewGuid();

        await fx.RegisterHandler.HandleAsync(@event.Id, registeredUser, "owner@example.com", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<NotFoundAppException>(
            () => fx.CancelHandler.HandleAsync(@event.Id, attackerUser, CancellationToken.None));
        Assert.NotNull(ex);

        var untouchedRegistration = fx.DbContext.EventRegistrations.Single(r => r.EventId == @event.Id && r.UserId == registeredUser);
        Assert.Equal(EventRegistrationStatus.Registered, untouchedRegistration.Status);
        Assert.DoesNotContain(fx.DbContext.EventRegistrations, r => r.UserId == attackerUser);
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
