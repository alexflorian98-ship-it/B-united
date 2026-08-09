using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Events.Application.UseCases.Admin;
using BUnited.Modules.Events.Domain;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Tests.TestSupport;

namespace BUnited.Modules.Events.Tests.Application;

public sealed class SetEventProgramsHandlerTests
{
    private sealed record Fixture(TestDbContext DbContext, FakeProgramLookup ProgramLookup, SetEventProgramsHandler Handler);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        var programLookup = new FakeProgramLookup();
        return new Fixture(context, programLookup, new SetEventProgramsHandler(context, programLookup));
    }

    private static Event SeedEvent(TestDbContext dbContext)
    {
        var @event = Event.Create("ro", DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(5).AddHours(1), "Europe/Bucharest", EventLocationType.Online, null, "https://meet.example.com/x", null, null);
        dbContext.Events.Add(@event);
        dbContext.SaveChanges();
        return @event;
    }

    [Fact]
    public async Task Associating_a_published_program_succeeds()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext);
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId, ProgramLookupStatus.Published);

        await fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [programId], Guid.NewGuid()), CancellationToken.None);

        Assert.Single(fx.DbContext.Set<EventProgram>().Where(ep => ep.EventId == @event.Id));
    }

    [Fact]
    public async Task Associating_a_non_published_program_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext);
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId, ProgramLookupStatus.Draft);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [programId], Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("EVENT_PROGRAM_NOT_PUBLISHED", ex.Code);
    }

    [Fact]
    public async Task Associating_a_non_existent_program_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext);

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [Guid.NewGuid()], Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Replacing_the_association_list_removes_programs_no_longer_included()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext);
        var programA = Guid.NewGuid();
        var programB = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programA);
        fx.ProgramLookup.AddProgram(programB);

        await fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [programA, programB], Guid.NewGuid()), CancellationToken.None);
        await fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [programB], Guid.NewGuid()), CancellationToken.None);

        var remaining = fx.DbContext.Set<EventProgram>().Where(ep => ep.EventId == @event.Id).ToList();
        Assert.Single(remaining);
        Assert.Equal(programB, remaining[0].ProgramId);
    }

    [Fact]
    public async Task Passing_an_empty_list_clears_all_associations()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;
        var @event = SeedEvent(fx.DbContext);
        var programId = Guid.NewGuid();
        fx.ProgramLookup.AddProgram(programId);
        await fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [programId], Guid.NewGuid()), CancellationToken.None);

        await fx.Handler.HandleAsync(new SetEventProgramsCommand(@event.Id, [], Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(fx.DbContext.Set<EventProgram>().Where(ep => ep.EventId == @event.Id));
    }
}
